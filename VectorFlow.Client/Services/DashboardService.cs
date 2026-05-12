using System.Net.Http.Json;
using VectorFlow.Shared.DTOs;
using VectorFlow.Client.Services.Interfaces;

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
}