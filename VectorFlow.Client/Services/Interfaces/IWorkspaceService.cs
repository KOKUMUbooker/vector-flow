using VectorFlow.Shared.DTOs;

namespace VectorFlow.Client.Services.Interfaces;
// ── Result wrapper ─────────────────────────────────────────────────────────────

// Convenience alias for void-like results
public class ServiceResult : ServiceResult<bool>
{
    public static ServiceResult Ok() =>
        new() { Succeeded = true, Data = true };

    public new static ServiceResult Failure(string error) =>
        new() { Error = error };

    public static ServiceResult NotFoundResult() =>
        new() { NotFound = true, Error = "Workspace not found." };

    public new static ServiceResult ForbiddenResult() =>
        new() { Forbidden = true, Error = "You don't have permission to perform this action." };
}

// ── Interface ──────────────────────────────────────────────────────────────────

public interface IWorkspaceService
{
    /// <summary>Returns all workspaces the current user belongs to.</summary>
    Task<ServiceResult<List<WorkspaceDto>>> GetWorkspacesAsync();

    /// <summary> Creates a workspace then returns the created workspace </summary>
    Task<ServiceResult<WorkspaceDto>> CreateWorkspaceAsync(CreateWorkspaceRequest request);

    /// <summary>Returns a single workspace by its URL slug.</summary>
    Task<ServiceResult<WorkspaceDto>> GetWorkspaceAsync(string slug);

    /// <summary>Returns all members of a workspace.</summary>
    Task<ServiceResult<List<WorkspaceMemberDto>>> GetMembersAsync(Guid workspaceId);

    /// <summary>Returns all projects of a workspace.</summary>
    Task<ServiceResult<List<ProjectDto>>> GetWorkspaceProjects(Guid workspaceId);

    /// <summary>Returns all invitations of a workspace.</summary>
    Task<ServiceResult<List<ProjectDto>>> GetWorkspaceInvitations(Guid workspaceId);

    /// <summary>Updates workspace name and description. Owner/Admin only.</summary>
    Task<ServiceResult<WorkspaceDto>> UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceRequest request);

    /// <summary>
    /// Permanently deletes a workspace and all its projects, issues, and members.
    /// Owner only.
    /// </summary>
    Task<ServiceResult> DeleteWorkspaceAsync(Guid workspaceId);
}