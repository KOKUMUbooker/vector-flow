using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VectorFlow.Api.DTOs;
using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Controllers;

[ApiController]
[Authorize]
public class LabelsController(ILabelService labelService) : ControllerBase
{
    // ── GET /api/projects/{projectId}/labels ──────────────────────────────────
    // Returns all labels for a project ordered alphabetically.
    // Any workspace member can view.

    [HttpGet("api/projects/{projectId:guid}/labels")]
    public async Task<IActionResult> GetLabels(Guid projectId)
    {
        var userId = GetUserId();
        var labels = await labelService.GetLabelsAsync(projectId, userId);
        return Ok(labels);
    }

    // ── POST /api/projects/{projectId}/labels ─────────────────────────────────
    // Creates a label scoped to the project. Owner/Admin only.

    [HttpPost("api/projects/{projectId:guid}/labels")]
    public async Task<IActionResult> CreateLabel(Guid projectId, CreateLabelRequest request)
    {
        var userId = GetUserId();
        var result = await labelService.CreateLabelAsync(projectId, request, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return CreatedAtAction(
            nameof(GetLabels),
            new { projectId },
            result.Label);
    }

    // ── PUT /api/labels/{labelId} ─────────────────────────────────────────────
    // Updates a label's name and color. Owner/Admin only.
    // Flat route — label ID is sufficient to authorise and resolve the project.

    [HttpPut("api/labels/{labelId:guid}")]
    public async Task<IActionResult> UpdateLabel(Guid labelId, UpdateLabelRequest request)
    {
        var userId = GetUserId();
        var result = await labelService.UpdateLabelAsync(labelId, request, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return Ok(result.Label);
    }

    // ── DELETE /api/labels/{labelId} ──────────────────────────────────────────
    // Deletes a label. Detaches it from all issues silently. Owner/Admin only.

    [HttpDelete("api/labels/{labelId:guid}")]
    public async Task<IActionResult> DeleteLabel(Guid labelId)
    {
        var userId = GetUserId();
        var result = await labelService.DeleteLabelAsync(labelId, userId);

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