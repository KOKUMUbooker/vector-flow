using System.Net;
using System.Net.Http.Json;
using VectorFlow.Client.Services.Interfaces;
using VectorFlow.Shared.DTOs;

namespace VectorFlow.Client.Services;

public class DashboardService(IHttpClientFactory httpClientFactory) : IDashboardService
{
    private HttpClient Http => httpClientFactory.CreateClient("VectorFlowApi");

    public async Task<DashboardDto?> GetDashboardAsync()
    {
        try
        {
            return await Http.GetFromJsonAsync<DashboardDto>("api/dashboard");
        }
        catch
        {
            return null;
        }
    }

    public async Task<ServiceResult<WorkspaceDetailsDashboardDto?>> GetDashboardWorkspaceDetailsAsync(string workspaceSlug)
    {
        try
        {
            var workspaceData = await Http.GetFromJsonAsync<WorkspaceDetailsDashboardDto?>($"api/dashboard/workspaces/{workspaceSlug}");
            return ServiceResult<WorkspaceDetailsDashboardDto?>.Success(workspaceData);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => ServiceResult<WorkspaceDetailsDashboardDto?>.Failure("Unauthorized."),
                _ => ServiceResult<WorkspaceDetailsDashboardDto?>.Failure("Failed to get workspace data.")
            };
        }
    }

    public async Task<ServiceResult<DashboardProjectData?>> GetDashboardProjectDataAsync(Guid projectId)
    {
        try
        {
            var workspaceData = await Http.GetFromJsonAsync<DashboardProjectData?>($"/api/dashboard/projects/{projectId}");
            return ServiceResult<DashboardProjectData?>.Success(workspaceData);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => ServiceResult<DashboardProjectData?>.Failure("Unauthorized."),
                _ => ServiceResult<DashboardProjectData?>.Failure("Failed to get project data.")
            };
        }
    }

    public async Task<ServiceResult<DashboardIssueData?>> GetDashboardIssueDataAsync(Guid projectId ,Guid issueId)
    {
        try
        {
            var workspaceData = await Http.GetFromJsonAsync<DashboardIssueData?>($"/api/dashboard/projects/{projectId}/issues/{issueId}");
            return ServiceResult<DashboardIssueData?>.Success(workspaceData);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => ServiceResult<DashboardIssueData?>.Failure("Unauthorized."),
                _ => ServiceResult<DashboardIssueData?>.Failure("Failed to get issue data.")
            };
        }
    }
}