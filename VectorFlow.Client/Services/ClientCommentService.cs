namespace VectorFlow.Client.Services;

using System.Net;
using System.Net.Http.Json;
using VectorFlow.Client.Services.Interfaces;
using VectorFlow.Shared.DTOs;

public class ClientCommentService(IHttpClientFactory httpClientFactory) : IClientCommentService
{
    private HttpClient Http => httpClientFactory.CreateClient("VectorFlowApi");

    // ── Get project comments  ──────────────────────────────────────────────

    public async Task<ServiceResult<List<CommentDto>>> GetCommentsAsync(Guid issueId)
    {
        try
        {
            var comments = await Http.GetFromJsonAsync<List<CommentDto>>(
                $"/api/issues/{issueId}/comments");

            return ServiceResult<List<CommentDto>>.Success(comments ?? []);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<List<CommentDto>>.NotFoundResult("Comments"),
                HttpStatusCode.Forbidden => ServiceResult<List<CommentDto>>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<List<CommentDto>>.Failure("Unauthorized."),
                _ => ServiceResult<List<CommentDto>>.Failure("Failed to load comments.")
            };
        }
    }

    // ── Create comment ─────────────────────────────────────────────────

    public async Task<ServiceResult<CommentDto>> CreateCommentAsync(Guid issueId, CreateCommentRequest request)
    {
        try
        {
            var response = await Http.PostAsJsonAsync<CreateCommentRequest>(
                $"/api/issues/{issueId}/comments", request);

            if (response.IsSuccessStatusCode)
            {
                var createdComment = await response.Content.ReadFromJsonAsync<CommentDto>();
                return ServiceResult<CommentDto>.Success(createdComment!);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<CommentDto>.NotFoundResult("Comment"),
                HttpStatusCode.Forbidden => ServiceResult<CommentDto>.ForbiddenResult(),
                _ => ServiceResult<CommentDto>.Failure(
                                                await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<CommentDto>.NotFoundResult("Comment"),
                HttpStatusCode.Forbidden => ServiceResult<CommentDto>.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult<CommentDto>.Failure("Unauthorized"),
                _ => ServiceResult<CommentDto>.Failure("Failed to create comment.")
            };
        }
    }

    // ── Update comment ─────────────────────────────────────────────────
    public async Task<ServiceResult<CommentDto>> UpdateCommentAsync(Guid commentId, UpdateCommentRequest request )
    {
        try {
            var response = await Http.PutAsJsonAsync<UpdateCommentRequest>(
                $"/api/comments/{commentId}", request);

            if (response.IsSuccessStatusCode)
            {
                var updatedComment = await response.Content.ReadFromJsonAsync<CommentDto>();
                return ServiceResult<CommentDto>.Success(updatedComment!);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult<CommentDto>.NotFoundResult("Comment"),
                HttpStatusCode.Forbidden => ServiceResult<CommentDto>.ForbiddenResult(),
                _ => ServiceResult<CommentDto>.Failure(await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                ? ServiceResult<CommentDto>.Failure("Unauthorized.")
                : ServiceResult<CommentDto>.Failure("Failed to update comment.");
        }
    }

    // ── Delete comment ─────────────────────────────────────────────────
    public async Task<ServiceResult> DeleteCommentAsync(Guid commentId)
    {
        try
        {
            var response = await Http.DeleteAsync($"/api/comments/{commentId}");

            if (response.IsSuccessStatusCode)
                return ServiceResult.Ok();

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult.NotFoundResult("Comment"),
                HttpStatusCode.Forbidden => ServiceResult.ForbiddenResult(),
                _ => ServiceResult.Failure(await ErrorUtil.ReadErrorMessageAsync(response))
            };
        }
        catch (HttpRequestException ex) {
            return ex.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceResult.NotFoundResult("Comment"),
                HttpStatusCode.Forbidden => ServiceResult.ForbiddenResult(),
                HttpStatusCode.Unauthorized => ServiceResult.Failure("Unauthorized."),
                _ => ServiceResult.Failure("Failed to delete comment.")
            };
        }
    }
}