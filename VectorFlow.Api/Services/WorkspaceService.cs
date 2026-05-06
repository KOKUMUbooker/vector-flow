using Microsoft.EntityFrameworkCore;
using VectorFlow.Api.Data;
using VectorFlow.Api.DTOs;
using VectorFlow.Api.Enums;
using VectorFlow.Api.Models;
using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Services;

public class WorkspaceService(AppDbContext db) : IWorkspaceService
{
    // ── Get all workspaces for a user ─────────────────────────────────────────

    public async Task<List<WorkspaceDto>> GetUserWorkspacesAsync(string userId)
    {
        return await db.WorkspaceMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Workspace)
                .ThenInclude(w => w.Members)
            .Include(m => m.Workspace)
                .ThenInclude(w => w.Projects)
            .Select(m => MapToDto(m.Workspace, m.Role))
            .ToListAsync();
    }

    // ── Get single workspace by slug ──────────────────────────────────────────

    public async Task<WorkspaceDto?> GetWorkspaceAsync(string slug, string userId)
    {
        var membership = await db.WorkspaceMembers
            .Include(m => m.Workspace)
                .ThenInclude(w => w.Members)
            .Include(m => m.Workspace)
                .ThenInclude(w => w.Projects)
            .FirstOrDefaultAsync(m =>
                m.Workspace.Slug == slug &&
                m.UserId == userId);

        return membership is null ? null : MapToDto(membership.Workspace, membership.Role);
    }

    // ── Create workspace ──────────────────────────────────────────────────────

    public async Task<WorkspaceResult> CreateWorkspaceAsync(
        CreateWorkspaceRequest request, string ownerId)
    {
        var slug = await GenerateUniqueSlugAsync(request.Name);

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description?.Trim(),
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };

        // Creator is automatically the Owner member
        var ownerMembership = new WorkspaceMember
        {
            WorkspaceId = workspace.Id,
            UserId = ownerId,
            Role = WorkspaceRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        await db.Workspaces.AddAsync(workspace);
        await db.WorkspaceMembers.AddAsync(ownerMembership);
        await db.SaveChangesAsync();

        // Reload with navigation properties for the response
        var created = await db.Workspaces
            .Include(w => w.Members)
            .Include(w => w.Projects)
            .FirstAsync(w => w.Id == workspace.Id);

        return WorkspaceResult.Success(MapToDto(created, WorkspaceRole.Owner));
    }

    // ── Update workspace ──────────────────────────────────────────────────────

    public async Task<WorkspaceResult> UpdateWorkspaceAsync(
        Guid workspaceId, UpdateWorkspaceRequest request, string requestingUserId)
    {
        var workspace = await db.Workspaces
            .Include(w => w.Members)
            .Include(w => w.Projects)
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace is null)
            return WorkspaceResult.Failure("Workspace not found.");

        var role = GetMemberRole(workspace, requestingUserId);

        if (role is null)
            return WorkspaceResult.Failure("You are not a member of this workspace.");

        if (role == WorkspaceRole.Member)
            return WorkspaceResult.Failure("Only Owners and Admins can update workspace settings.");

        // Regenerate slug only if the name actually changed
        if (!workspace.Name.Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase))
            workspace.Slug = await GenerateUniqueSlugAsync(request.Name, workspaceId);

        workspace.Name = request.Name.Trim();
        workspace.Description = request.Description?.Trim();

        await db.SaveChangesAsync();

        return WorkspaceResult.Success(MapToDto(workspace, role.Value));
    }

    // ── Delete workspace ──────────────────────────────────────────────────────

    public async Task<MemberResult> DeleteWorkspaceAsync(
        Guid workspaceId, string requestingUserId)
    {
        var workspace = await db.Workspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace is null)
            return MemberResult.Failure("Workspace not found.");

        // Only the original Owner can delete the workspace
        if (workspace.OwnerId != requestingUserId)
            return MemberResult.Failure("Only the workspace Owner can delete it.");

        db.Workspaces.Remove(workspace); // cascade handles members, projects, issues
        await db.SaveChangesAsync();

        return MemberResult.Success();
    }

    // ── Get members ───────────────────────────────────────────────────────────

    public async Task<List<WorkspaceMemberDto>> GetMembersAsync(
        Guid workspaceId, string requestingUserId)
    {
        // Verify the requester is a member before returning the list
        var isMember = await db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == requestingUserId);

        if (!isMember) return [];

        var workspace = await db.Workspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        return await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId)
            .Include(m => m.User)
            .OrderBy(m => m.JoinedAt)
            .Select(m => new WorkspaceMemberDto
            {
                UserId = m.UserId,
                DisplayName = m.User.DisplayName,
                Email = m.User.Email!,
                AvatarUrl = m.User.AvatarUrl,
                Role = m.Role,
                JoinedAt = m.JoinedAt,
                IsOwner = workspace != null && m.UserId == workspace.OwnerId
            })
            .ToListAsync();
    }

    // ── Update member role ────────────────────────────────────────────────────

    public async Task<MemberResult> UpdateMemberRoleAsync(
        Guid workspaceId, string targetUserId, WorkspaceRole newRole, string requestingUserId)
    {
        var workspace = await db.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace is null)
            return MemberResult.Failure("Workspace not found.");

        var requesterRole = GetMemberRole(workspace, requestingUserId);
        var targetMember = workspace.Members.FirstOrDefault(m => m.UserId == targetUserId);

        if (requesterRole is null)
            return MemberResult.Failure("You are not a member of this workspace.");

        if (targetMember is null)
            return MemberResult.Failure("Target user is not a member of this workspace.");

        // Cannot change the owner's role under any circumstance
        if (targetUserId == workspace.OwnerId)
            return MemberResult.Failure("The workspace Owner's role cannot be changed.");

        // Cannot change your own role
        if (targetUserId == requestingUserId)
            return MemberResult.Failure("You cannot change your own role.");

        // Admins cannot promote/demote other Admins — only the Owner can
        if (requesterRole == WorkspaceRole.Admin && targetMember.Role == WorkspaceRole.Admin)
            return MemberResult.Failure("Admins cannot change the role of other Admins.");

        // Members have no management rights
        if (requesterRole == WorkspaceRole.Member)
            return MemberResult.Failure("You do not have permission to change member roles.");

        // Prevent anyone from assigning the Owner role via this endpoint
        if (newRole == WorkspaceRole.Owner)
            return MemberResult.Failure("The Owner role cannot be assigned.");

        targetMember.Role = newRole;
        await db.SaveChangesAsync();

        return MemberResult.Success();
    }

    // ── Remove member ─────────────────────────────────────────────────────────

    public async Task<MemberResult> RemoveMemberAsync(
        Guid workspaceId, string targetUserId, string requestingUserId)
    {
        var workspace = await db.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace is null)
            return MemberResult.Failure("Workspace not found.");

        var requesterRole = GetMemberRole(workspace, requestingUserId);
        var targetMember = workspace.Members.FirstOrDefault(m => m.UserId == targetUserId);

        if (requesterRole is null)
            return MemberResult.Failure("You are not a member of this workspace.");

        if (targetMember is null)
            return MemberResult.Failure("Target user is not a member of this workspace.");

        if (requesterRole == WorkspaceRole.Member)
            return MemberResult.Failure("You do not have permission to remove members.");

        // The Owner cannot be removed
        if (targetUserId == workspace.OwnerId)
            return MemberResult.Failure("The workspace Owner cannot be removed.");

        // Admins cannot remove other Admins — only the Owner can
        if (requesterRole == WorkspaceRole.Admin && targetMember.Role == WorkspaceRole.Admin)
            return MemberResult.Failure("Admins cannot remove other Admins.");

        db.WorkspaceMembers.Remove(targetMember);
        await db.SaveChangesAsync();

        return MemberResult.Success();
    }

    // ── Leave workspace ───────────────────────────────────────────────────────

    public async Task<MemberResult> LeaveWorkspaceAsync(Guid workspaceId, string userId)
    {
        var workspace = await db.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace is null)
            return MemberResult.Failure("Workspace not found.");

        // Owner cannot leave — they must delete the workspace or transfer ownership first
        if (workspace.OwnerId == userId)
            return MemberResult.Failure("The workspace Owner cannot leave. Delete the workspace instead.");

        var membership = workspace.Members.FirstOrDefault(m => m.UserId == userId);

        if (membership is null)
            return MemberResult.Failure("You are not a member of this workspace.");

        db.WorkspaceMembers.Remove(membership);
        await db.SaveChangesAsync();

        return MemberResult.Success();
    }

    // ── Get user role ─────────────────────────────────────────────────────────

    public async Task<WorkspaceRole?> GetUserRoleAsync(Guid workspaceId, string userId)
    {
        var member = await db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

        return member?.Role;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Gets the role of a user from an already-loaded workspace.Members collection.
    /// Avoids an extra DB query when the members are already in memory.
    /// </summary>
    private static WorkspaceRole? GetMemberRole(Workspace workspace, string userId) =>
        workspace.Members.FirstOrDefault(m => m.UserId == userId)?.Role;

    /// <summary>
    /// Generates a URL-friendly slug from a workspace name.
    /// Appends a numeric suffix if the slug is already taken.
    /// Excludes the current workspace when checking for uniqueness (for updates).
    /// </summary>
    private async Task<string> GenerateUniqueSlugAsync(string name, Guid? excludeId = null)
    {
        var baseSlug = name.Trim().ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        // Strip any character that isn't a letter, digit, or hyphen
        baseSlug = new string(baseSlug
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .ToArray());

        // Collapse consecutive hyphens
        while (baseSlug.Contains("--"))
            baseSlug = baseSlug.Replace("--", "-");

        baseSlug = baseSlug.Trim('-');

        var slug = baseSlug;
        var suffix = 1;

        while (await db.Workspaces
            .AnyAsync(w => w.Slug == slug && (excludeId == null || w.Id != excludeId)))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }

    private static WorkspaceDto MapToDto(Workspace workspace, WorkspaceRole currentUserRole) =>
        new()
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Slug = workspace.Slug,
            Description = workspace.Description,
            OwnerId = workspace.OwnerId,
            CreatedAt = workspace.CreatedAt,
            CurrentUserRole = currentUserRole,
            MemberCount = workspace.Members.Count,
            ProjectCount = workspace.Projects.Count
        };
}