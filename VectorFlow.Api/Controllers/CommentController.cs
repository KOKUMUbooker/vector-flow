using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VectorFlow.Shared.DTOs;
using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Controllers;

[ApiController]
[Authorize]
public class CommentsController(ICommentService commentService) : ControllerBase
{
    // ── GET /api/issues/{issueId}/comments ────────────────────────────────────
    // Returns all comments for an issue ordered oldest-first.
    // Any workspace member can view.

    [HttpGet("api/issues/{issueId:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid issueId)
    {
        var userId = GetUserId();
        var comments = await commentService.GetCommentsAsync(issueId, userId);
        return Ok(comments);
    }

    // ── POST /api/issues/{issueId}/comments ───────────────────────────────────
    // Adds a comment to an issue.
    // Any workspace member can comment.

    [HttpPost("api/issues/{issueId:guid}/comments")]
    public async Task<IActionResult> CreateComment(Guid issueId, CreateCommentRequest request)
    {
        var userId = GetUserId();
        var result = await commentService.CreateCommentAsync(issueId, request, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return CreatedAtAction(
            nameof(GetComments),
            new { issueId },
            result.Comment);
    }

    // ── PUT /api/comments/{commentId} ─────────────────────────────────────────
    // Edits a comment. Author only.
    // Note: comment routes are flat (not nested under issues) since
    // the comment ID is sufficient to identify and authorise the operation.

    [HttpPut("api/comments/{commentId:guid}")]
    public async Task<IActionResult> UpdateComment(Guid commentId, UpdateCommentRequest request)
    {
        var userId = GetUserId();
        var result = await commentService.UpdateCommentAsync(commentId, request, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return Ok(result.Comment);
    }

    // ── DELETE /api/comments/{commentId} ──────────────────────────────────────
    // Deletes a comment. Author, Owner, or Admin only.

    [HttpDelete("api/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid commentId)
    {
        var userId = GetUserId();
        var result = await commentService.DeleteCommentAsync(commentId, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return NoContent();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found in token.");

    private IActionResult ToErrorResponse(string error)
    {
        if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return NotFound(new { Message = error });

        if (error.Contains("only", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("not a member", StringComparison.OrdinalIgnoreCase))
            return StatusCode(403, new { Message = error });

        return BadRequest(new { Message = error });
    }
}