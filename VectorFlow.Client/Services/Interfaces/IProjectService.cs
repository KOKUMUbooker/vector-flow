namespace VectorFlow.Client.Services.Interfaces;

using VectorFlow.Shared.DTOs;

public interface IProjectService
{
    /// <summary>Returns all projects of a workspace.</summary>
    Task<ServiceResult<List<ProjectDto>>> GetWorkspaceProjects(Guid workspaceId);

    /// <summary> Creates a project then returns the created project </summary>
    Task<ServiceResult<ProjectDto>> CreateProjectAsync(Guid workspaceId, CreateProjectRequest request);

    /// <summary>Returns a single project.</summary>
    Task<ServiceResult<ProjectDto>> GetProjectAsync(Guid workspaceId, Guid projectId);

    Task<ServiceResult> DeleteProjectAsync(Guid workspaceId, Guid projectId);
    Task<ServiceResult<ProjectDto>> UpdateProjectAsync(Guid workspaceId, Guid projectId, UpdateProjectRequest request);
}