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

    public async Task<ServiceResult<WorkspaceDetailsDashboardDto?>> GetDashboardWorkspaceDetailsAsync(Guid workspaceId)
    {
        try
        {
            var workspaceData = await Http.GetFromJsonAsync<WorkspaceDetailsDashboardDto?>($"api/dashboard/workspaces/{workspaceId}");
            return ServiceResult<WorkspaceDetailsDashboardDto?>.Success(workspaceData);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => ServiceResult<WorkspaceDetailsDashboardDto?>.Failure("Session expired. Please sign in again."),
                _ => ServiceResult<WorkspaceDetailsDashboardDto?>.Failure("Failed to get workspace data.")
            };
        }
    }
}