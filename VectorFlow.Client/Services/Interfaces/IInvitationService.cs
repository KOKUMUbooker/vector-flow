namespace VectorFlow.Client.Services.Interfaces;

using VectorFlow.Shared.DTOs;

public interface IInvitationService
{
    /// <summary>Returns all invitations of a workspace.</summary>
    Task<ServiceResult<List<InvitationDto>>> GetWorkspaceInvitations(Guid workspaceId);
    Task<ServiceResult<List<InvitationDto>>> GetMyInvitations();

    /// <summary> Creates a project then returns the created project </summary>
    Task<ServiceResult<InvitationDto>> SendInvitationAsync(Guid workspaceId, SendInvitationRequest request);

    Task<ServiceResult> DeleteInvitationAsync(Guid invitationId);
    Task<ServiceResult<InvitationActionRes>> AcceptInvitationAsync(string token);
    Task<ServiceResult<InvitationActionRes>> DeclineInvitationAsync(string token);
}

public class InvitationActionRes : MessageRes
{
    public string? WorkspaceId { get; set; } = string.Empty;
}