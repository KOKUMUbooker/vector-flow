using VectorFlow.Shared.DTOs;

namespace VectorFlow.Client.Services.Interfaces;
// ── Result wrapper ─────────────────────────────────────────────────────────────

/// <summary>
/// Generic result type used by all workspace service methods.
/// Avoids throwing exceptions for expected failure cases (404, 403, validation errors)
/// so the UI can branch cleanly without try/catch everywhere.
/// </summary>
public class ServiceResult<T>
{
    public bool Succeeded { get; protected set; }
    public T? Data { get; protected set; }
    public string? Error { get; protected set; }
    public bool NotFound { get; protected set; }
    public bool Forbidden { get; protected set; }

    public static ServiceResult<T> Success(T data) =>
        new() { Succeeded = true, Data = data };

    public static ServiceResult<T> Failure(string error) =>
        new() { Error = error };

    public static ServiceResult<T> NotFoundResult() =>
        new() { NotFound = true, Error = "Workspace not found." };

    public static ServiceResult<T> ForbiddenResult() =>
        new() { Forbidden = true, Error = "You don't have permission to perform this action." };
}

// Convenience alias for void-like results
public class ServiceResult : ServiceResult<bool>
{
    public static ServiceResult Ok() =>
        new() { Succeeded = true, Data = true };

    public new static ServiceResult Failure(string error) =>
        new() { Error = error };

    public new static ServiceResult NotFoundResult() =>
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

    /// <summary>Updates workspace name and description. Owner/Admin only.</summary>
    Task<ServiceResult<WorkspaceDto>> UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceRequest request);

    /// <summary>
    /// Permanently deletes a workspace and all its projects, issues, and members.
    /// Owner only.
    /// </summary>
    Task<ServiceResult> DeleteWorkspaceAsync(Guid workspaceId);
}