using VectorFlow.Api.DTOs;

namespace VectorFlow.Api.Services.Interfaces;

public interface IInvitationService
{
    /// <summary>
    /// Sends an invitation email to the given address.
    /// Rules enforced:
    /// - Requester must be Owner or Admin.
    /// - Email must belong to an existing VectorFlow user.
    /// - User must not already be a member of the workspace.
    /// - Any previous pending invite for this email in this workspace is cancelled first.
    /// </summary>
    Task<InvitationResult> SendInvitationAsync(
        Guid workspaceId, SendInvitationRequest request, string requestingUserId, string baseUrl);

    /// <summary>
    /// Returns all invitations for a workspace (all statuses).
    /// Owner/Admin only.
    /// </summary>
    Task<List<InvitationDto>> GetWorkspaceInvitationsAsync(Guid workspaceId, string requestingUserId);

    /// <summary>
    /// Returns all pending invitations for the authenticated user's email.
    /// Used to show the user any workspaces they've been invited to.
    /// </summary>
    Task<List<InvitationDto>> GetMyInvitationsAsync(string userId);

    /// <summary>
    /// Validates the token and adds the user to the workspace as a Member.
    /// The invitation must be Pending and not expired.
    /// The accepting user's email must match the invited email.
    /// </summary>
    Task<InvitationResult> AcceptInvitationAsync(string token, string userId);

    /// <summary>
    /// Marks the invitation as Declined. The invited user can no longer accept it.
    /// </summary>
    Task<InvitationResult> DeclineInvitationAsync(string token, string userId);

    /// <summary>
    /// Cancels a pending invitation. Owner/Admin only.
    /// Only Pending invitations can be cancelled.
    /// </summary>
    Task<InvitationResult> CancelInvitationAsync(Guid invitationId, string requestingUserId);
}