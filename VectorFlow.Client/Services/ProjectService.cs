namespace VectorFlow.Client.Services;

using System.Net;
using System.Net.Http.Json;
using VectorFlow.Client.Services.Interfaces;
using VectorFlow.Shared.DTOs;

public class ProjectService(IHttpClientFactory httpClientFactory) : IProjectService
{
    private HttpClient Http => httpClientFactory.CreateClient("VectorFlowApi");


    // ── Get workspace projects  ──────────────────────────────────────────────

    public async Task<ServiceResult<List<ProjectDto>>> GetWorkspaceProjects(Guid workspaceId)
    {
        try
        {
            var projects = await Http.GetFromJsonAsync<List<ProjectDto>>(
                $"api/workspaces/{workspaceId}/projects");

            return ServiceResult<List<ProjectDto>>.Success(projects ?? []);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<List<ProjectDto>>.NotFoundResult("Project"),
                HttpStatusCode.Forbidden => ServiceResult<List<ProjectDto>>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<List<ProjectDto>>.Failure("Session expired."),
                _ => ServiceResult<List<ProjectDto>>.Failure("Failed to fetch projects.")
            };
        }
    }

    // ── Create project ─────────────────────────────────────────────────

    public async Task<ServiceResult<ProjectDto>> CreateProjectAsync(Guid workspaceId, CreateProjectRequest request)
    {
        try
        {
            var response = await Http.PostAsJsonAsync<CreateProjectRequest>($"api/workspaces/{workspaceId}/projects", request);

            if (response.IsSuccessStatusCode)
            {
                var createdProject = await response.Content.ReadFromJsonAsync<ProjectDto>();
                return ServiceResult<ProjectDto>.Success(createdProject!);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<ProjectDto>.NotFoundResult("Project"),
                HttpStatusCode.Forbidden => ServiceResult<ProjectDto>.ForbiddenResult(),
                _ => ServiceResult<ProjectDto>.Failure(
                                                await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<ProjectDto>.NotFoundResult("Project"),
                HttpStatusCode.Forbidden => ServiceResult<ProjectDto>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<ProjectDto>.Failure("Session expired. Please sign in again."),
                _ => ServiceResult<ProjectDto>.Failure("Failed to create project.")
            };
        }
    }

   
    // ── Get single project ─────────────────────────────────────────────────

    public async Task<ServiceResult<ProjectDto>> GetProjectAsync(Guid workspaceId, Guid projectId) {
        try
        {
            var project = await Http.GetFromJsonAsync<ProjectDto>(
                $"/api/workspaces/{workspaceId}/projects/{projectId}");

            return ServiceResult<ProjectDto>.Success(project!);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<ProjectDto>.NotFoundResult("Project"),
                HttpStatusCode.Forbidden => ServiceResult<ProjectDto>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<ProjectDto>.Failure("Session expired."),
                _ => ServiceResult<ProjectDto>.Failure("Failed to fetch project.")
            };
        }
    }

    // ── Delete project by id ─────────────────────────────────────────────────

    public async Task<ServiceResult> DeleteProjectAsync(Guid workspaceId, Guid projectId) {
        var response = await Http.DeleteAsync($"/api/workspaces/{workspaceId}/projects/{projectId}");

        if (response.IsSuccessStatusCode)
            return ServiceResult.Ok();

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => ServiceResult.NotFoundResult("Project"),
            HttpStatusCode.Forbidden => ServiceResult.ForbiddenResult(),
            _ => ServiceResult.Failure(await ErrorUtil.ReadErrorMessageAsync(response))
        };
    }

    // ── Update project ─────────────────────────────────────────────────

    public async Task<ServiceResult<ProjectDto>> UpdateProjectAsync(Guid workspaceId, Guid projectId, UpdateProjectRequest request) {
        try
        {
            var response = await Http.PutAsJsonAsync<UpdateProjectRequest>($"api/workspaces/{workspaceId}/projects/{projectId}", request);

            if (response.IsSuccessStatusCode)
            {
                var updated = await response.Content.ReadFromJsonAsync<ProjectDto>();
                return ServiceResult<ProjectDto>.Success(updated!);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<ProjectDto>.NotFoundResult("Project"),
                HttpStatusCode.Forbidden => ServiceResult<ProjectDto>.ForbiddenResult(),
                _ => ServiceResult<ProjectDto>.Failure(await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                ? ServiceResult<ProjectDto>.Failure("Session expired. Please sign in again.")
                : ServiceResult<ProjectDto>.Failure("Failed to update project.");
        }
    }
}