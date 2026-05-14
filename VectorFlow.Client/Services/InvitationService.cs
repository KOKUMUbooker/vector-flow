namespace VectorFlow.Client.Services;

using System.Net;
using System.Net.Http.Json;
using VectorFlow.Client.Services.Interfaces;
using VectorFlow.Shared.DTOs;

public class InvitationService(IHttpClientFactory httpClientFactory) : IInvitationService
{
    private HttpClient Http => httpClientFactory.CreateClient("VectorFlowApi");

    // ── Get workspace invitations  ──────────────────────────────────────────────

    public async Task<ServiceResult<List<InvitationDto>>> GetWorkspaceInvitations(Guid workspaceId)
    {
        try
        {
            var invitations = await Http.GetFromJsonAsync<List<InvitationDto>>(
                $"/api/workspaces/{workspaceId}/invitations");

            return ServiceResult<List<InvitationDto>>.Success(invitations ?? []);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<List<InvitationDto>>.NotFoundResult("Invitations"),
                HttpStatusCode.Forbidden => ServiceResult<List<InvitationDto>>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<List<InvitationDto>>.Failure("Session expired."),
                _ => ServiceResult<List<InvitationDto>>.Failure("Failed to load invitations.")
            };
        }
    }

    public async Task<ServiceResult<List<InvitationDto>>> GetMyInvitations()
    {
        try
        {
            var invitations = await Http.GetFromJsonAsync<List<InvitationDto>>("api/invitations/mine");

            return ServiceResult<List<InvitationDto>>.Success(invitations ?? []);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<List<InvitationDto>>.NotFoundResult("Invitations"),
                HttpStatusCode.Forbidden => ServiceResult<List<InvitationDto>>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<List<InvitationDto>>.Failure("Session expired."),
                _ => ServiceResult<List<InvitationDto>>.Failure("Failed to load invitations.")
            };
        }
    }

    // ── Send Invitation ─────────────────────────────────────────────────

    public async Task<ServiceResult<InvitationDto>> SendInvitationAsync(Guid workspaceId, SendInvitationRequest request)
    {
        try
        {
            var response = await Http.PostAsJsonAsync<SendInvitationRequest>($"/api/workspaces/{workspaceId}/invitations", request);

            if (response.IsSuccessStatusCode)
            {
                var createdInvitation = await response.Content.ReadFromJsonAsync<InvitationDto>();
                return ServiceResult<InvitationDto>.Success(createdInvitation!);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<InvitationDto>.NotFoundResult("Invitation"),
                HttpStatusCode.Forbidden => ServiceResult<InvitationDto>.ForbiddenResult(),
                _ => ServiceResult<InvitationDto>.Failure(
                                                await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<InvitationDto>.NotFoundResult("Invitation"),
                HttpStatusCode.Forbidden => ServiceResult<InvitationDto>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<InvitationDto>.Failure("Session expired. Please sign in again."),
                _ => ServiceResult<InvitationDto>.Failure("Failed to create invitation.")
            };
        }
    }

    public async Task<ServiceResult> DeleteInvitationAsync( Guid invitationId) {
        try
        {
            var response = await Http.DeleteAsync($"/api/invitations/{invitationId}");

            if (response.IsSuccessStatusCode)
                return ServiceResult.Ok();

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult.NotFoundResult("Invitation"),
                HttpStatusCode.Forbidden => ServiceResult.ForbiddenResult(),
                _ => ServiceResult.Failure(await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex) {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult.NotFoundResult("Invitation"),
                HttpStatusCode.Forbidden => ServiceResult.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult.Failure("Session expired. Please sign in again."),
                _ => ServiceResult.Failure("Failed to delete invitation.")
            };
        }
    }

    public async Task<ServiceResult<InvitationActionRes>> AcceptInvitationAsync(string token) {
        try {
            var response = await Http.PostAsJsonAsync($"/api/invitations/accept?token={token}", new {});

            if (response.IsSuccessStatusCode)
            {
                var acceptRes = await response.Content.ReadFromJsonAsync<InvitationActionRes>();
                return ServiceResult<InvitationActionRes>.Success(acceptRes!);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<InvitationActionRes>.NotFoundResult("Invitation"),
                HttpStatusCode.Forbidden => ServiceResult<InvitationActionRes>.ForbiddenResult(),
                _ => ServiceResult<InvitationActionRes>.Failure(await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                ? ServiceResult<InvitationActionRes>.Failure("Session expired. Please sign in again.")
                : ServiceResult<InvitationActionRes>.Failure("Failed to accept invitation.");
        }
    }

    public async Task<ServiceResult<InvitationActionRes>> DeclineInvitationAsync(string token) {
        try
        {
            var response = await Http.PostAsJsonAsync($"/api/invitations/decline?token={token}", new { });

            if (response.IsSuccessStatusCode)
            {
                var declineRes = await response.Content.ReadFromJsonAsync<InvitationActionRes>();
                return ServiceResult<InvitationActionRes>.Success(declineRes!);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<InvitationActionRes>.NotFoundResult("Invitation"),
                HttpStatusCode.Forbidden => ServiceResult<InvitationActionRes>.ForbiddenResult(),
                _ => ServiceResult<InvitationActionRes>.Failure(await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                ? ServiceResult<InvitationActionRes>.Failure("Session expired. Please sign in again.")
                : ServiceResult<InvitationActionRes>.Failure("Failed to decline invitation.");

        }
    }
}

