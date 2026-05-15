using VectorFlow.Shared.DTOs;

namespace VectorFlow.Client.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto?> GetDashboardAsync();
    Task<ServiceResult<WorkspaceDetailsDashboardDto?>> GetDashboardWorkspaceDetailsAsync(string workspaceSlug);
    Task<ServiceResult<DashboardProjectData?>> GetDashboardProjectDataAsync(Guid projectId);
    Task<ServiceResult<DashboardIssueData?>> GetDashboardIssueDataAsync(Guid projectId, Guid issueId);
}