using VectorFlow.Api.DTOs;

namespace VectorFlow.Api.Services.Interfaces;

public interface IProjectService
{
    /// <summary>
    /// Returns all projects in a workspace.
    /// User must be a workspace member to view projects.
    /// </summary>
    Task<List<ProjectDto>> GetProjectsAsync(Guid workspaceId, string requestingUserId);

    /// <summary>
    /// Returns a single project by ID.
    /// User must be a workspace member.
    /// </summary>
    Task<ProjectDto?> GetProjectAsync(Guid projectId, string requestingUserId);

    /// <summary>
    /// Creates a new project inside a workspace.
    /// Owner/Admin only.
    /// KeyPrefix must be unique within the workspace.
    /// </summary>
    Task<ProjectResult> CreateProjectAsync(Guid workspaceId, CreateProjectRequest request, string requestingUserId);

    /// <summary>
    /// Updates project name and description.
    /// Owner/Admin only. KeyPrefix cannot be changed after creation
    /// to avoid breaking existing issue keys.
    /// </summary>
    Task<ProjectResult> UpdateProjectAsync(Guid projectId, UpdateProjectRequest request, string requestingUserId);

    /// <summary>
    /// Permanently deletes the project and all its issues, comments,
    /// activity logs and labels via cascade.
    /// Owner/Admin only.
    /// </summary>
    Task<ProjectResult> DeleteProjectAsync(Guid projectId, string requestingUserId);
}