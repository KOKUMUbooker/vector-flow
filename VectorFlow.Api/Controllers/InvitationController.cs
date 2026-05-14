using DotNetEnv;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VectorFlow.Api.Services.Interfaces;
using VectorFlow.Shared.DTOs;

namespace VectorFlow.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class InvitationsController(IInvitationService invitationService) : ControllerBase
{
    // ── POST /api/workspaces/{id}/invitations ─────────────────────────────────
    // Send an invitation. Owner/Admin only.

    [HttpPost("workspaces/{workspaceId:guid}/invitations")]
    public async Task<IActionResult> SendInvitation(
        Guid workspaceId, SendInvitationRequest request)
    {
        var userId = GetUserId();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await invitationService.SendInvitationAsync(workspaceId, request, userId, baseUrl);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return Ok(result.Invitation);
    }

    // ── GET /api/workspaces/{id}/invitations ──────────────────────────────────
    // List all invitations for a workspace. Owner/Admin only.

    [HttpGet("workspaces/{workspaceId:guid}/invitations")]
    public async Task<IActionResult> GetWorkspaceInvitations(Guid workspaceId)
    {
        var userId = GetUserId();
        var invitations = await invitationService.GetWorkspaceInvitationsAsync(workspaceId, userId);
        return Ok(invitations);
    }

    // ── GET /api/invitations/mine ─────────────────────────────────────────────
    // Returns all pending invitations addressed to the authenticated user.
    // Used on the client dashboard to show "You have pending invitations".

    [HttpGet("invitations/mine")]
    public async Task<IActionResult> GetMyInvitations()
    {
        var userId = GetUserId();
        var invitations = await invitationService.GetMyInvitationsAsync(userId);
        return Ok(invitations);
    }

    // ── POST /api/invitations/accept ──────────────────────────────────────────
    // Accept an invitation via token from the email link.
    // Token is passed as a query param since this is opened from an email.

    [HttpPost("invitations/accept")]
    [HttpGet("invitations/accept")]
    public async Task<IActionResult> AcceptInvitation([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { Message = "Invitation token is required." });

        var userId = GetUserId();
        var result = await invitationService.AcceptInvitationAsync(token, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        // If request was made via GET request - ie user clicked email link
        if (HttpMethods.IsGet(Request.Method))
        {
            return Ok($"You have joined {result.Invitation!.WorkspaceName}. please proceed to the app");
        }

        return Ok(new
        {
            Message = $"You have joined {result.Invitation!.WorkspaceName}.",
            result.Invitation.WorkspaceId
        });
    }

    // ── POST /api/invitations/decline ─────────────────────────────────────────
    // Decline an invitation via token.

    [HttpPost("invitations/decline")]
    public async Task<IActionResult> DeclineInvitation([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { Message = "Invitation token is required." });

        var userId = GetUserId();
        var result = await invitationService.DeclineInvitationAsync(token, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return Ok(new { Message = "Invitation declined." });
    }

    // ── DELETE /api/workspaces/{id}/invitations/{invitationId} ────────────────
    // Cancel a pending invitation. Owner/Admin only.

    [HttpDelete("invitations/{invitationId:guid}")]
    public async Task<IActionResult> CancelInvitation(Guid invitationId)
    {
        var userId = GetUserId();
        var result = await invitationService.CancelInvitationAsync(invitationId, userId);

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

        if (error.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("only", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("cannot", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("different email", StringComparison.OrdinalIgnoreCase))
            return StatusCode(403, new { Message = error });

        return BadRequest(new { Message = error });
    }
}