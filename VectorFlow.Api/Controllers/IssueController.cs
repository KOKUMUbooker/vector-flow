using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VectorFlow.Shared.DTOs;
using VectorFlow.Api.Enums;
using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/issues")]
[Authorize]
public class IssuesController(IIssueService issueService) : ControllerBase
{
    // ── GET /api/projects/{projectId}/issues ──────────────────────────────────
    // Returns all issues in a project, with optional filters.
    // Used by both the Kanban board and the list view.

    [HttpGet]
    public async Task<IActionResult> GetIssues(
        Guid projectId,
        [FromQuery] IssueStatus? status,
        [FromQuery] IssuePriority? priority,
        [FromQuery] IssueType? type,
        [FromQuery] string? assigneeId)
    {
        var userId = GetUserId();
        var issues = await issueService.GetIssuesAsync(
            projectId, userId, status, priority, type, assigneeId);
        return Ok(issues);
    }

    // ── GET /api/projects/{projectId}/issues/{issueId} ────────────────────────
    // Returns a single issue with full detail.

    [HttpGet("{issueId:guid}")]
    public async Task<IActionResult> GetIssue(Guid projectId, Guid issueId)
    {
        var userId = GetUserId();
        var issue = await issueService.GetIssueAsync(issueId, userId);
        return issue is null ? NotFound() : Ok(issue);
    }

    // ── GET /api/projects/{projectId}/issues/{issueId}/activity ──────────────
    // Returns the activity log for an issue.

    [HttpGet("{issueId:guid}/activity")]
    public async Task<IActionResult> GetActivityLogs(Guid projectId, Guid issueId)
    {
        var userId = GetUserId();
        var logs = await issueService.GetActivityLogsAsync(issueId, userId);
        return Ok(logs);
    }

    // ── POST /api/projects/{projectId}/issues ─────────────────────────────────
    // Creates a new issue. Any workspace member can create.

    [HttpPost]
    public async Task<IActionResult> CreateIssue(Guid projectId, CreateIssueRequest request)
    {
        var userId = GetUserId();
        var result = await issueService.CreateIssueAsync(projectId, request, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return CreatedAtAction(
            nameof(GetIssue),
            new { projectId, issueId = result.Issue!.Id },
            result.Issue);
    }

    // ── PUT /api/projects/{projectId}/issues/{issueId} ────────────────────────
    // Updates issue fields. Reporter, Owner, Admin only.

    [HttpPut("{issueId:guid}")]
    public async Task<IActionResult> UpdateIssue(
        Guid projectId, Guid issueId, UpdateIssueRequest request)
    {
        var userId = GetUserId();
        var result = await issueService.UpdateIssueAsync(issueId, request, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return Ok(result.Issue);
    }

    // ── PATCH /api/projects/{projectId}/issues/{issueId}/status ──────────────
    // Updates status only. Used by Kanban board drag-and-drop.
    // Any workspace member can update status.

    [HttpPatch("{issueId:guid}/status")]
    public async Task<IActionResult> UpdateIssueStatus(
        Guid projectId, Guid issueId, UpdateIssueStatusRequest request)
    {
        var userId = GetUserId();
        var result = await issueService.UpdateIssueStatusAsync(issueId, request, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return Ok(result.Issue);
    }

    // ── PATCH /api/projects/{projectId}/issues/{issueId}/position ─────────────
    // Updates position within a column. Used by Kanban card reordering.
    // Any workspace member can reorder.

    [HttpPatch("{issueId:guid}/position")]
    public async Task<IActionResult> UpdateIssuePosition(
        Guid projectId, Guid issueId, UpdateIssuePositionRequest request)
    {
        var userId = GetUserId();
        var result = await issueService.UpdateIssuePositionAsync(issueId, request, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return Ok(result.Issue);
    }

    // ── DELETE /api/projects/{projectId}/issues/{issueId} ─────────────────────
    // Deletes an issue. Reporter, Owner, Admin only.

    [HttpDelete("{issueId:guid}")]
    public async Task<IActionResult> DeleteIssue(Guid projectId, Guid issueId)
    {
        var userId = GetUserId();
        var result = await issueService.DeleteIssueAsync(issueId, userId);

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
            error.Contains("not a member", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("you can only", StringComparison.OrdinalIgnoreCase))
            return StatusCode(403, new { Message = error });

        return BadRequest(new { Message = error });
    }
}