namespace VectorFlow.Client.Services;

using System.Net;
using System.Net.Http.Json;
using VectorFlow.Client.Services.Interfaces;
using VectorFlow.Shared.DTOs;

public class ClientLabelService(IHttpClientFactory httpClientFactory) : IClientLabelService
{
    private HttpClient Http => httpClientFactory.CreateClient("VectorFlowApi");

    // ── Get project labels  ──────────────────────────────────────────────

    public async Task<ServiceResult<List<LabelDto>>> GetLabelsAsync(Guid projectId)
    {
        try
        {
            var labels = await Http.GetFromJsonAsync<List<LabelDto>>(
                $"api/projects/{projectId}/labels");

            return ServiceResult<List<LabelDto>>.Success(labels ?? []);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<List<LabelDto>>.NotFoundResult("Labels"),
                HttpStatusCode.Forbidden => ServiceResult<List<LabelDto>>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<List<LabelDto>>.Failure("Unauthorized."),
                _ => ServiceResult<List<LabelDto>>.Failure("Failed to load labels.")
            };
        }
    }

    // ── Create label ─────────────────────────────────────────────────

    public async Task<ServiceResult<LabelDto>> CreateLabelAsync(Guid projectId, CreateLabelRequest request)
    {
        try
        {
            var response = await Http.PostAsJsonAsync<CreateLabelRequest>(
                $"/api/projects/{projectId}/labels", request);

            if (response.IsSuccessStatusCode)
            {
                var createdLabel = await response.Content.ReadFromJsonAsync<LabelDto>();
                return ServiceResult<LabelDto>.Success(createdLabel!);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<LabelDto>.NotFoundResult("Label"),
                HttpStatusCode.Forbidden => ServiceResult<LabelDto>.ForbiddenResult(),
                _ => ServiceResult<LabelDto>.Failure(
                                                await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<LabelDto>.NotFoundResult("Label"),
                HttpStatusCode.Forbidden => ServiceResult<LabelDto>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<LabelDto>.Failure("Unauthorized"),
                _ => ServiceResult<LabelDto>.Failure("Failed to create label.")
            };
        }
    }

    // ── Update label ─────────────────────────────────────────────────
    public async Task<ServiceResult<LabelDto>> UpdateLabelAsync(Guid labelId, UpdateLabelRequest request)
    {
        try {
            var response = await Http.PutAsJsonAsync<UpdateLabelRequest>(
                $"/api/labels/{labelId}", request);

            if (response.IsSuccessStatusCode)
            {
                var updatedLabel = await response.Content.ReadFromJsonAsync<LabelDto>();
                return ServiceResult<LabelDto>.Success(updatedLabel!);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<LabelDto>.NotFoundResult("Label"),
                HttpStatusCode.Forbidden => ServiceResult<LabelDto>.ForbiddenResult(),
                _ => ServiceResult<LabelDto>.Failure(await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                ? ServiceResult<LabelDto>.Failure("Unauthorized.")
                : ServiceResult<LabelDto>.Failure("Failed to update label.");
        }
    }

    // ── Delete label ─────────────────────────────────────────────────
    public async Task<ServiceResult> DeleteLabelAsync(Guid labelId)
    {
        try
        {
            var response = await Http.DeleteAsync($"api/labels/{labelId}");

            if (response.IsSuccessStatusCode)
                return ServiceResult.Ok();

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult.NotFoundResult("Label"),
                HttpStatusCode.Forbidden => ServiceResult.ForbiddenResult(),
                _ => ServiceResult.Failure(await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex) {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult.NotFoundResult("Label"),
                HttpStatusCode.Forbidden => ServiceResult.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult.Failure("Unauthorized."),
                _ => ServiceResult.Failure("Failed to delete label.")
            };
        }
    }
}