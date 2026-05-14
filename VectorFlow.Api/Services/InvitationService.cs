using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VectorFlow.Api.Data;
using VectorFlow.Shared.DTOs;
using VectorFlow.Shared.Enums;
using VectorFlow.Api.Models;
using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Services;

public class InvitationService(
    AppDbContext db,
    UserManager<AppUser> userManager,
    IEmailService emailService,
    IEmailTemplateService templateService) : IInvitationService
{
    // ── Send invitation ───────────────────────────────────────────────────────

    public async Task<InvitationResult> SendInvitationAsync(
        Guid workspaceId,
        SendInvitationRequest request,
        string requestingUserId,
        string baseUrl)
    {
        var workspace = await db.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace is null)
            return InvitationResult.Failure("Workspace not found.");

        // Requester must be Owner or Admin
        var requesterRole = workspace.Members
            .FirstOrDefault(m => m.UserId == requestingUserId)?.Role;

        if (requesterRole is null)
            return InvitationResult.Failure("You are not a member of this workspace.");

        if (requesterRole == WorkspaceRole.Member)
            return InvitationResult.Failure("Only Owners and Admins can send invitations.");

        var email = request.Email.Trim().ToLowerInvariant();

        // Only existing VectorFlow users can be invited
        var invitedUser = await userManager.FindByEmailAsync(email);
        if (invitedUser is null)
            return InvitationResult.Failure(
                "No VectorFlow account found for that email address. " +
                "The user must register before they can be invited.");

        // Cannot invite someone already in the workspace
        var alreadyMember = workspace.Members.Any(m => m.UserId == invitedUser.Id);
        if (alreadyMember)
            return InvitationResult.Failure("That user is already a member of this workspace.");

        // Cancel any existing pending invitation for this email in this workspace
        // before creating a new one — allows re-inviting after expiry or decline
        var existingPending = await db.Invitations
            .FirstOrDefaultAsync(i =>
                i.WorkspaceId == workspaceId &&
                i.InvitedEmail == email &&
                i.Status == InvitationStatus.Pending);

        if (existingPending is not null)
            existingPending.Status = InvitationStatus.Expired;

        var inviter = await userManager.FindByIdAsync(requestingUserId);

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            InvitedEmail = email,
            InvitedByUserId = requestingUserId,
            Token = GenerateSecureToken(),
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await db.Invitations.AddAsync(invitation);
        await db.SaveChangesAsync();

        // Send invitation email
        var inviteUrl = $"{baseUrl}/invitations/accept?token={invitation.Token}";
        var html = await templateService.GenerateInvitationEmail(
            "VectorFlow",
            invitedUser.DisplayName,
            inviter!.DisplayName,
            workspace.Name,
            inviteUrl);

        await emailService.SendEmailAsync(email, $"You've been invited to {workspace.Name}", html);

        return InvitationResult.Success(MapToDto(invitation, workspace));
    }

    // ── Get workspace invitations ─────────────────────────────────────────────

    public async Task<List<InvitationDto>> GetWorkspaceInvitationsAsync(
        Guid workspaceId, string requestingUserId)
    {
        var role = await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == requestingUserId)
            .Select(m => (WorkspaceRole?)m.Role)
            .FirstOrDefaultAsync();

        if (role is null || role == WorkspaceRole.Member)
            return [];

        return await db.Invitations
            .Where(i => i.WorkspaceId == workspaceId)
            .Include(i => i.Workspace)
            .Include(i => i.InvitedBy)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => MapToDto(i, i.Workspace))
            .ToListAsync();
    }

    // ── Get my invitations ────────────────────────────────────────────────────

    public async Task<List<InvitationDto>> GetMyInvitationsAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return [];

        // Expire any invitations that have passed their expiry date
        var expiredInvitations = await db.Invitations
            .Where(i =>
                i.InvitedEmail == user.Email!.ToLower() &&
                i.Status == InvitationStatus.Pending &&
                i.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var expired in expiredInvitations)
            expired.Status = InvitationStatus.Expired;

        if (expiredInvitations.Any())
            await db.SaveChangesAsync();

        return await db.Invitations
            .Where(i =>
                i.InvitedEmail == user.Email!.ToLower() &&
                i.Status == InvitationStatus.Pending)
            .Include(i => i.Workspace)
            .Include(i => i.InvitedBy)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => MapToDto(i, i.Workspace))
            .ToListAsync();
    }

    // ── Accept invitation ─────────────────────────────────────────────────────

    public async Task<InvitationResult> AcceptInvitationAsync(string token, string userId)
    {
        var invitation = await db.Invitations
            .Include(i => i.Workspace)
                .ThenInclude(w => w.Members)
            .Include(i => i.InvitedBy)
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invitation is null)
            return InvitationResult.Failure("Invalid invitation link.");

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return InvitationResult.Failure("User not found.");

        // The accepting user's email must match the invited email
        if (!string.Equals(user.Email, invitation.InvitedEmail, StringComparison.OrdinalIgnoreCase))
            return InvitationResult.Failure("This invitation was sent to a different email address.");

        if (invitation.Status == InvitationStatus.Accepted)
            return InvitationResult.Failure("This invitation has already been accepted.");

        if (invitation.Status != InvitationStatus.Pending)
            return InvitationResult.Failure("This invitation is no longer valid.");

        if (invitation.IsExpired)
        {
            invitation.Status = InvitationStatus.Expired;
            await db.SaveChangesAsync();
            return InvitationResult.Failure("This invitation has expired. Please ask for a new one.");
        }

        // Guard against the user already being a member (e.g. joined via another invite)
        var alreadyMember = invitation.Workspace.Members
            .Any(m => m.UserId == userId);

        if (alreadyMember)
        {
            invitation.Status = InvitationStatus.Accepted;
            await db.SaveChangesAsync();
            return InvitationResult.Failure("You are already a member of this workspace.");
        }

        // Add to workspace as Member
        var membership = new WorkspaceMember
        {
            WorkspaceId = invitation.WorkspaceId,
            UserId = userId,
            Role = WorkspaceRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        invitation.Status = InvitationStatus.Accepted;

        await db.WorkspaceMembers.AddAsync(membership);
        await db.SaveChangesAsync();

        return InvitationResult.Success(MapToDto(invitation, invitation.Workspace));
    }

    // ── Decline invitation ────────────────────────────────────────────────────

    public async Task<InvitationResult> DeclineInvitationAsync(string token, string userId)
    {
        var invitation = await db.Invitations
            .Include(i => i.Workspace)
            .Include(i => i.InvitedBy)
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invitation is null)
            return InvitationResult.Failure("Invalid invitation link.");

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return InvitationResult.Failure("User not found.");

        if (!string.Equals(user.Email, invitation.InvitedEmail, StringComparison.OrdinalIgnoreCase))
            return InvitationResult.Failure("This invitation was sent to a different email address.");

        if (invitation.Status != InvitationStatus.Pending)
            return InvitationResult.Failure("This invitation is no longer valid.");

        invitation.Status = InvitationStatus.Declined;
        await db.SaveChangesAsync();

        return InvitationResult.Success(MapToDto(invitation, invitation.Workspace));
    }

    // ── Cancel invitation ─────────────────────────────────────────────────────

    public async Task<InvitationResult> CancelInvitationAsync(
        Guid invitationId, string requestingUserId)
    {
        var invitation = await db.Invitations
            .Include(i => i.Workspace)
                .ThenInclude(w => w.Members)
            .Include(i => i.InvitedBy)
            .FirstOrDefaultAsync(i => i.Id == invitationId);

        if (invitation is null)
            return InvitationResult.Failure("Invitation not found.");

        var requesterRole = invitation.Workspace.Members
            .FirstOrDefault(m => m.UserId == requestingUserId)?.Role;

        if (requesterRole is null)
            return InvitationResult.Failure("You are not a member of this workspace.");

        if (requesterRole == WorkspaceRole.Member)
            return InvitationResult.Failure("Only Owners and Admins can cancel invitations.");

        if (invitation.Status != InvitationStatus.Pending)
            return InvitationResult.Failure("Only pending invitations can be cancelled.");

        invitation.Status = InvitationStatus.Expired;
        await db.SaveChangesAsync();

        return InvitationResult.Success(MapToDto(invitation, invitation.Workspace));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string GenerateSecureToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

    private static InvitationDto MapToDto(Invitation invitation, Workspace workspace) =>
        new()
        {
            Id = invitation.Id,
            WorkspaceId = invitation.WorkspaceId,
            WorkspaceName = workspace.Name,
            WorkspaceSlug = workspace.Slug,
            InvitedEmail = invitation.InvitedEmail,
            InvitedByDisplayName = invitation.InvitedBy?.DisplayName ?? "A workspace admin",
            Status = invitation.Status,
            ExpiresAt = invitation.ExpiresAt,
            CreatedAt = invitation.CreatedAt,
            Token = invitation.Token,
        };
}