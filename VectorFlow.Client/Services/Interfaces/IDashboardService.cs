using VectorFlow.Shared.DTOs;

namespace VectorFlow.Client.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto?> GetDashboardAsync();
    Task<ServiceResult<WorkspaceDetailsDashboardDto?>> GetDashboardWorkspaceDetailsAsync(Guid workspaceId);
}