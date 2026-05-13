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
                                                await ErrorUtil.ReadErrorMessageAsync(response))
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

    // ── Remove member from workspace ───────────────────────────────────────────────────
   
    public async Task<ServiceResult> RemoveMemberAsync(Guid workspaceId, string targetUserId)
    {
        try
        {
            var response = await Http.DeleteAsync($"/api/workspaces/{workspaceId}/members/{targetUserId}");

            if (response.IsSuccessStatusCode)
                return ServiceResult.Ok();

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult.NotFoundResult("Workspace member"),
                HttpStatusCode.Forbidden => ServiceResult.ForbiddenResult(),
                _ => ServiceResult.Failure(
                                                await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                ? ServiceResult.Failure("Session expired. Please sign in again.")
                : ServiceResult.Failure("Failed to remove member from workspace.");
        }
    }

    // ── Remove member from workspace ───────────────────────────────────────────────────

    public async Task<ServiceResult> LeaveWorkspaceAsync(Guid workspaceId)
    {
        try
        {
            var response = await Http.DeleteAsync($"/api/workspaces/{workspaceId}/members/me");

            if (response.IsSuccessStatusCode)
                return ServiceResult.Ok();

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult.NotFoundResult("Your member account"),
                HttpStatusCode.Forbidden => ServiceResult.ForbiddenResult(),
                _ => ServiceResult.Failure(
                                                await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                ? ServiceResult.Failure("Session expired. Please sign in again.")
                : ServiceResult.Failure("Failed to remove your account from the workspace.");
        }
    }

    // ── Update workspace member role ───────────────────────────────────────────────────

    public async Task<ServiceResult<MessageRes>> UpdateMemberRoleAsync(Guid workspaceId, string targetUserId, UpdateMemberRoleRequest request)
    {
        try
        {
            var response = await Http.PutAsJsonAsync($"api/workspaces/{workspaceId}", request);

            if (response.IsSuccessStatusCode)
            {
                var msgRes = await response.Content.ReadFromJsonAsync<MessageRes>();
                return ServiceResult<MessageRes>.Success(msgRes!);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<MessageRes>.NotFoundResult("Workspace"),
                HttpStatusCode.Forbidden => ServiceResult<MessageRes>.ForbiddenResult(),
                _ => ServiceResult<MessageRes>.Failure(
                                                await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                ? ServiceResult<MessageRes>.Failure("Session expired. Please sign in again.")
                : ServiceResult<MessageRes>.Failure("Failed to update workspace.");
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
                                                await ErrorUtil.ReadErrorMessageAsync(response))
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
                HttpStatusCode.NotFound => ServiceResult.NotFoundResult("Workspace"),
                HttpStatusCode.Forbidden => ServiceResult.ForbiddenResult(),
                _ => ServiceResult.Failure(
                                                await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                ? ServiceResult.Failure("Session expired. Please sign in again.")
                : ServiceResult.Failure("Failed to delete workspace.");
        }
    }
}
