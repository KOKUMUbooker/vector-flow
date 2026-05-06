using VectorFlow.Api.DTOs;
using VectorFlow.Api.Enums;

namespace VectorFlow.Api.Services.Interfaces;

public interface IWorkspaceService
{
    // ── Workspace CRUD ────────────────────────────────────────────────────────

    /// <summary>Returns all workspaces the user is a member of.</summary>
    Task<List<WorkspaceDto>> GetUserWorkspacesAsync(string userId);

    /// <summary>Returns a single workspace by slug if the user is a member.</summary>
    Task<WorkspaceDto?> GetWorkspaceAsync(string slug, string userId);

    /// <summary>Creates a new workspace and assigns the creator as Owner.</summary>
    Task<WorkspaceResult> CreateWorkspaceAsync(CreateWorkspaceRequest request, string ownerId);

    /// <summary>Updates workspace name and description. Owner/Admin only.</summary>
    Task<WorkspaceResult> UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceRequest request, string requestingUserId);

    /// <summary>
    /// Permanently deletes the workspace and all its projects and issues.
    /// Owner only.
    /// </summary>
    Task<MemberResult> DeleteWorkspaceAsync(Guid workspaceId, string requestingUserId);

    // ── Member management ─────────────────────────────────────────────────────

    /// <summary>Returns all members of a workspace.</summary>
    Task<List<WorkspaceMemberDto>> GetMembersAsync(Guid workspaceId, string requestingUserId);

    /// <summary>
    /// Changes a member's role. Owner/Admin only.
    /// Admins cannot change the Owner's role or another Admin's role.
    /// Only the Owner can promote/demote Admins.
    /// </summary>
    Task<MemberResult> UpdateMemberRoleAsync(Guid workspaceId, string targetUserId, WorkspaceRole newRole, string requestingUserId);

    /// <summary>
    /// Removes a member from the workspace. Owner/Admin only.
    /// The Owner cannot be removed.
    /// </summary>
    Task<MemberResult> RemoveMemberAsync(Guid workspaceId, string targetUserId, string requestingUserId);

    /// <summary>Allows a member to leave a workspace themselves. Owner cannot leave.</summary>
    Task<MemberResult> LeaveWorkspaceAsync(Guid workspaceId, string userId);

    // ── Helpers used by other services ───────────────────────────────────────

    /// <summary>Returns the role of a user in a workspace. Null if not a member.</summary>
    Task<WorkspaceRole?> GetUserRoleAsync(Guid workspaceId, string userId);
}