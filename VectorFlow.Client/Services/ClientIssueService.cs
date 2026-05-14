namespace VectorFlow.Client.Services;

using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Net.Http.Json;
using VectorFlow.Client.Services.Interfaces;
using VectorFlow.Shared.DTOs;
using VectorFlow.Shared.Enums;

public class ClientIssueService(IHttpClientFactory httpClientFactory) : IClientIssueService
{
    private HttpClient Http => httpClientFactory.CreateClient("VectorFlowApi");

    public async Task<ServiceResult<List<IssueDto>>> GetIssuesAsync(
        Guid projectId,
        string requestingUserId,
        IssueStatus? status = null,
        IssuePriority? priority = null,
        IssueType? type = null,
        string? assigneeId = null)
    {
        try
        {
            var url = $"/api/projects/{projectId}/issues";

            var queryParams = new Dictionary<string, string?>();

            if (status.HasValue)
                queryParams["status"] = status.Value.ToString();

            if (priority.HasValue)
                queryParams["priority"] = priority.Value.ToString();

            if (type.HasValue)
                queryParams["type"] = type.Value.ToString();

            if (!string.IsNullOrWhiteSpace(assigneeId))
                queryParams["assigneeId"] = assigneeId;

            url = QueryHelpers.AddQueryString(url, queryParams);

            var issues = await Http.GetFromJsonAsync<List<IssueDto>>(url);

            return ServiceResult<List<IssueDto>>.Success(issues ?? []);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<List<IssueDto>>.NotFoundResult("Issues"),
                HttpStatusCode.Forbidden => ServiceResult<List<IssueDto>>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<List<IssueDto>>.Failure("Unauthorized"),
                _ => ServiceResult<List<IssueDto>>.Failure("Failed to load issues.")
            };
        }
    }

    public async Task<ServiceResult<IssueDto?>> GetIssueAsync(Guid projectId , Guid issueId)
    {
        try
        {
            var issue = await Http.GetFromJsonAsync<IssueDto?>($"/api/projects/{projectId}/issues/{issueId}");

            return issue is null
                ? ServiceResult<IssueDto?>.NotFoundResult("Issue")
                : ServiceResult<IssueDto?>.Success(issue);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<IssueDto?>.NotFoundResult("Issue"),
                HttpStatusCode.Forbidden => ServiceResult<IssueDto?>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<IssueDto?>.Failure("Session expired. Please sign in again."),
                _ => ServiceResult<IssueDto?>.Failure("Failed to load issue.")
            };
        }
    }

    public async Task<ServiceResult<List<ActivityLogDto>>> GetActivityLogsAsync(Guid projectId, Guid issueId)
    {
        try
        {
            var logs = await Http.GetFromJsonAsync<List<ActivityLogDto>>(
                $"/api/projects/{projectId}/issues/{issueId}/activity");

            return ServiceResult<List<ActivityLogDto>>.Success(logs ?? []);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<List<ActivityLogDto>>.NotFoundResult("Activity logs"),
                HttpStatusCode.Forbidden => ServiceResult<List<ActivityLogDto>>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<List<ActivityLogDto>>.Failure("Session expired."),
                _ => ServiceResult<List<ActivityLogDto>>.Failure("Failed to load activity logs.")
            };
        }
    }

    public async Task<ServiceResult<IssueDto?>> CreateIssueAsync(Guid projectId, CreateIssueRequest request)
    {
        try
        {
            var response = await Http.PostAsJsonAsync<CreateIssueRequest>(
                $"/api/projects/{projectId}/issues", request);

            if (response.IsSuccessStatusCode)
            {
                var createdIssue = await response.Content.ReadFromJsonAsync<IssueDto?>();
                return ServiceResult<IssueDto?>.Success(createdIssue!);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<IssueDto?>.NotFoundResult("Issue"),
                HttpStatusCode.Forbidden => ServiceResult<IssueDto?>.ForbiddenResult(),
                _ => ServiceResult<IssueDto?>.Failure(
                                                await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<IssueDto?>.NotFoundResult("Issue"),
                HttpStatusCode.Forbidden => ServiceResult<IssueDto?>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<IssueDto?>.Failure("Unauthorized."),
                _ => ServiceResult<IssueDto?>.Failure("Failed to create issue.")
            };
        }
    }

    public async Task<ServiceResult<IssueDto>> UpdateIssueAsync(Guid projectId, Guid issueId, UpdateIssueRequest request)
    {
        try
        {
            var response = await Http.PutAsJsonAsync<UpdateIssueRequest>(
                $"/api/projects/{projectId}/issues/{issueId}", request);

            if (response.IsSuccessStatusCode)
            {
                var updated = await response.Content.ReadFromJsonAsync<IssueDto>();
                return ServiceResult<IssueDto>.Success(updated!);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<IssueDto>.NotFoundResult("Issue"),
                HttpStatusCode.Forbidden => ServiceResult<IssueDto>.ForbiddenResult(),
                _ => ServiceResult<IssueDto>.Failure(await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                ? ServiceResult<IssueDto>.Failure("Unauthorized.")
                : ServiceResult<IssueDto>.Failure("Failed to update project.");
        }
    }

    public async Task<ServiceResult<IssueDto>> UpdateIssueStatusAsync(Guid projectId, Guid issueId, UpdateIssueStatusRequest request)
    {
        try
        {
            var response = await Http.PatchAsJsonAsync<UpdateIssueStatusRequest>(
                $"/api/projects/{projectId}/issues/{issueId}/status", request);

            if (response.IsSuccessStatusCode)
            {
                var updated = await response.Content.ReadFromJsonAsync<IssueDto>();
                return ServiceResult<IssueDto>.Success(updated!);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<IssueDto>.NotFoundResult("Issue"),
                HttpStatusCode.Forbidden => ServiceResult<IssueDto>.ForbiddenResult(),
                _ => ServiceResult<IssueDto>.Failure(await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                ? ServiceResult<IssueDto>.Failure("Unauthorized.")
                : ServiceResult<IssueDto>.Failure("Failed to update project.");
        }
    }

    public async Task<ServiceResult<IssueDto>> UpdateIssuePositionAsync(Guid projectId, Guid issueId, UpdateIssuePositionRequest request)
    {
        try
        {
            var response = await Http.PatchAsJsonAsync<UpdateIssuePositionRequest>(
                $"/api/projects/{projectId}/issues/{issueId}/position", request);

            if (response.IsSuccessStatusCode)
            {
                var updated = await response.Content.ReadFromJsonAsync<IssueDto>();
                return ServiceResult<IssueDto>.Success(updated!);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<IssueDto>.NotFoundResult("Issue"),
                HttpStatusCode.Forbidden => ServiceResult<IssueDto>.ForbiddenResult(),
                _ => ServiceResult<IssueDto>.Failure(await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                ? ServiceResult<IssueDto>.Failure("Unauthorized.")
                : ServiceResult<IssueDto>.Failure("Failed to update project.");
        }
    }

    public async Task<ServiceResult> DeleteIssueAsync(Guid projectId, Guid issueId)
    {
        try
        {
            var response = await Http.DeleteAsync($"/api/projects/{projectId}/issues/{issueId}");

            if (response.IsSuccessStatusCode)
                return ServiceResult.Ok();

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult.NotFoundResult("Issue"),
                HttpStatusCode.Forbidden => ServiceResult.ForbiddenResult(),
                _ => ServiceResult.Failure(await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult.NotFoundResult("Invitation"),
                HttpStatusCode.Forbidden => ServiceResult.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult.Failure("Unauthorized"),
                _ => ServiceResult.Failure("Failed to delete issue.")
            };
        }
    }
}
