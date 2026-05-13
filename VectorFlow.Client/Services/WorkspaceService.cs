using System.Net;
using System.Net.Http.Json;
using VectorFlow.Shared.DTOs;
using VectorFlow.Client.Services.Interfaces;

namespace VectorFlow.Client.Services;

public class WorkspaceService(IHttpClientFactory httpClientFactory) : IWorkspaceService
{
    private HttpClient Http => httpClientFactory.CreateClient("VectorFlowApi");

    // ── Get all workspaces ─────────────────────────────────────────────────

    public async Task<ServiceResult<List<WorkspaceDto>>> GetWorkspacesAsync()
    {
        try
        {
            var workspaces = await Http.GetFromJsonAsync<List<WorkspaceDto>>("api/workspaces");
            return ServiceResult<List<WorkspaceDto>>.Success(workspaces ?? []);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => ServiceResult<List<WorkspaceDto>>.Failure("Session expired. Please sign in again."),
                _ => ServiceResult<List<WorkspaceDto>>.Failure("Failed to load workspaces.")
            };
        }
    }

    // ── Create workspace ─────────────────────────────────────────────────

    public async Task<ServiceResult<WorkspaceDto>> CreateWorkspaceAsync(CreateWorkspaceRequest request)
    {
        try
        {
            var response = await Http.PostAsJsonAsync<CreateWorkspaceRequest>("api/workspaces", request);
            
            if (response.IsSuccessStatusCode)
            {
                var createdWorkspace = await response.Content.ReadFromJsonAsync<WorkspaceDto>();
                return ServiceResult<WorkspaceDto>.Success(createdWorkspace!);
            }

            return response.StatusCode switch
            {   
                HttpStatusCode.NotFound => ServiceResult<WorkspaceDto>.NotFoundResult("Workspace"),
                HttpStatusCode.Forbidden => ServiceResult<WorkspaceDto>.ForbiddenResult(),
                _ => ServiceResult<WorkspaceDto>.Failure(
                                                await ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<WorkspaceDto>.NotFoundResult("Workspace"),
                HttpStatusCode.Forbidden => ServiceResult<WorkspaceDto>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<WorkspaceDto>.Failure("Session expired. Please sign in again."),
                _ => ServiceResult<WorkspaceDto>.Failure("Failed to create workspace.")
            };
        }
    }

    // ── Get single workspace by slug ───────────────────────────────────────

    public async Task<ServiceResult<WorkspaceDto>> GetWorkspaceAsync(string slug)
    {
        try
        {
            var workspace = await Http.GetFromJsonAsync<WorkspaceDto>($"api/workspaces/{slug}");

            return workspace is null
                ? ServiceResult<WorkspaceDto>.NotFoundResult("Workspace")
                : ServiceResult<WorkspaceDto>.Success(workspace);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<WorkspaceDto>.NotFoundResult("Workspace"),
                HttpStatusCode.Forbidden => ServiceResult<WorkspaceDto>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<WorkspaceDto>.Failure("Session expired. Please sign in again."),
                _ => ServiceResult<WorkspaceDto>.Failure("Failed to load workspace.")
            };
        }
    }

    // ── Get workspace members ──────────────────────────────────────────────

    public async Task<ServiceResult<List<WorkspaceMemberDto>>> GetMembersAsync(Guid workspaceId)
    {
        try
        {
            var members = await Http.GetFromJsonAsync<List<WorkspaceMemberDto>>(
                $"api/workspaces/{workspaceId}/members");

            return ServiceResult<List<WorkspaceMemberDto>>.Success(members ?? []);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<List<WorkspaceMemberDto>>.NotFoundResult("Workspace"),
                HttpStatusCode.Forbidden => ServiceResult<List<WorkspaceMemberDto>>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<List<WorkspaceMemberDto>>.Failure("Session expired."),
                _ => ServiceResult<List<WorkspaceMemberDto>>.Failure("Failed to load members.")
            };
        }
    }

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
                HttpStatusCode.NotFound => ServiceResult<List<ProjectDto>>.NotFoundResult("Workspace"),
                HttpStatusCode.Forbidden => ServiceResult<List<ProjectDto>>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<List<ProjectDto>>.Failure("Session expired."),
                _ => ServiceResult<List<ProjectDto>>.Failure("Failed to load members.")
            };
        }
    }

    // ── Get workspace invitations  ──────────────────────────────────────────────

    public async Task<ServiceResult<List<ProjectDto>>> GetWorkspaceInvitations(Guid workspaceId)
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
                HttpStatusCode.NotFound => ServiceResult<List<ProjectDto>>.NotFoundResult("Workspace"),
                HttpStatusCode.Forbidden => ServiceResult<List<ProjectDto>>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<List<ProjectDto>>.Failure("Session expired."),
                _ => ServiceResult<List<ProjectDto>>.Failure("Failed to load members.")
            };
        }
    }

    // ── Update workspace ───────────────────────────────────────────────────

    public async Task<ServiceResult<WorkspaceDto>> UpdateWorkspaceAsync(
        Guid workspaceId, UpdateWorkspaceRequest request)
    {
        try
        {
            var response = await Http.PutAsJsonAsync($"api/workspaces/{workspaceId}", request);

            if (response.IsSuccessStatusCode)
            {
                var updated = await response.Content.ReadFromJsonAsync<WorkspaceDto>();
                return ServiceResult<WorkspaceDto>.Success(updated!);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<WorkspaceDto>.NotFoundResult("Workspace"),
                HttpStatusCode.Forbidden => ServiceResult<WorkspaceDto>.ForbiddenResult(),
                _ => ServiceResult<WorkspaceDto>.Failure(
                                                await ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                ? ServiceResult<WorkspaceDto>.Failure("Session expired. Please sign in again.")
                : ServiceResult<WorkspaceDto>.Failure("Failed to update workspace.");
        }
    }

    // ── Delete workspace ───────────────────────────────────────────────────

    public async Task<ServiceResult> DeleteWorkspaceAsync(Guid workspaceId)
    {
        try
        {
            var response = await Http.DeleteAsync($"api/workspaces/{workspaceId}");

            if (response.IsSuccessStatusCode)
                return ServiceResult.Ok();

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult.NotFoundResult(),
                HttpStatusCode.Forbidden => ServiceResult.ForbiddenResult(),
                _ => ServiceResult.Failure(
                                                await ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                ? ServiceResult.Failure("Session expired. Please sign in again.")
                : ServiceResult.Failure("Failed to delete workspace.");
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Tries to read a { "message": "..." } body from an error response.
    /// Falls back to a generic message if the body can't be parsed.
    /// </summary>
    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
            return body?.Message ?? "An unexpected error occurred.";
        }
        catch
        {
            return "An unexpected error occurred.";
        }
    }

    private record ErrorBody(string? Message);
}
