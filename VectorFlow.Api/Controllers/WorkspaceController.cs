using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VectorFlow.Shared.DTOs;
using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
[Authorize] // All workspace endpoints require authentication
public class WorkspacesController(IWorkspaceService workspaceService) : ControllerBase
{
    // ── GET /api/workspaces ───────────────────────────────────────────────────
    // Returns all workspaces the authenticated user belongs to.
    // Used by the client on login to decide where to route the user.

    [HttpGet]
    public async Task<IActionResult> GetMyWorkspaces()
    {
        var userId = GetUserId();
        var workspaces = await workspaceService.GetUserWorkspacesAsync(userId);
        return Ok(workspaces);
    }

    // ── GET /api/workspaces/{slug} ────────────────────────────────────────────
    // Returns a single workspace by its URL slug.
    // Returns 404 if the user is not a member — does not reveal the workspace exists.

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetWorkspace(string slug)
    {
        var userId = GetUserId();
        var workspace = await workspaceService.GetWorkspaceAsync(slug, userId);

        return workspace is null ? NotFound() : Ok(workspace);
    }

    // ── POST /api/workspaces ──────────────────────────────────────────────────
    // Creates a new workspace. The authenticated user becomes the Owner.

    [HttpPost]
    public async Task<IActionResult> CreateWorkspace(CreateWorkspaceRequest request)
    {
        var userId = GetUserId();
        var result = await workspaceService.CreateWorkspaceAsync(request, userId);

        if (!result.Succeeded)
            return BadRequest(new { Message = result.Error });

        return CreatedAtAction(
            nameof(GetWorkspace),
            new { slug = result.Workspace!.Slug },
            result.Workspace);
    }

    // ── PUT /api/workspaces/{id} ──────────────────────────────────────────────
    // Updates workspace name and description. Owner/Admin only.

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateWorkspace(Guid id, UpdateWorkspaceRequest request)
    {
        var userId = GetUserId();
        var result = await workspaceService.UpdateWorkspaceAsync(id, request, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return Ok(result.Workspace);
    }

    // ── DELETE /api/workspaces/{id} ───────────────────────────────────────────
    // Permanently deletes the workspace and all its data. Owner only.

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteWorkspace(Guid id)
    {
        var userId = GetUserId();
        var result = await workspaceService.DeleteWorkspaceAsync(id, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return NoContent();
    }

    // ── GET /api/workspaces/{id}/members ─────────────────────────────────────
    // Returns all members of a workspace. Any member can view.

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        var userId = GetUserId();
        var members = await workspaceService.GetMembersAsync(id, userId);

        // Empty list means either no members or user is not a member
        if (!members.Any())
            return NotFound();

        return Ok(members);
    }

    // ── PUT /api/workspaces/{id}/members/{userId}/role ────────────────────────
    // Changes a member's role. Owner/Admin only.
    // Role promotion/demotion rules are enforced in the service.

    [HttpPut("{id:guid}/members/{targetUserId}/role")]
    public async Task<IActionResult> UpdateMemberRole(
        Guid id,
        string targetUserId,
        UpdateMemberRoleRequest request)
    {
        var requestingUserId = GetUserId();
        var result = await workspaceService.UpdateMemberRoleAsync(
            id, targetUserId, request.Role, requestingUserId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return Ok(new { Message = "Member role updated." });
    }

    // ── DELETE /api/workspaces/{id}/members/{userId} ──────────────────────────
    // Removes a member from the workspace. Owner/Admin only.
    // The Owner cannot be removed.

    [HttpDelete("{id:guid}/members/{targetUserId}")]
    public async Task<IActionResult> RemoveMember(Guid id, string targetUserId)
    {
        var requestingUserId = GetUserId();
        var result = await workspaceService.RemoveMemberAsync(id, targetUserId, requestingUserId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return NoContent();
    }

    // ── DELETE /api/workspaces/{id}/members/me ────────────────────────────────
    // Allows the authenticated user to leave a workspace.
    // Owner cannot leave — must delete the workspace instead.

    [HttpDelete("{id:guid}/members/me")]
    public async Task<IActionResult> LeaveWorkspace(Guid id)
    {
        var userId = GetUserId();
        var result = await workspaceService.LeaveWorkspaceAsync(id, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return NoContent();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found in token.");

    /// <summary>
    /// Maps service error messages to appropriate HTTP status codes.
    /// Keeps error handling consistent without leaking internal details.
    /// </summary>
    private IActionResult ToErrorResponse(string error)
    {
        if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return NotFound(new { Message = error });

        if (error.Contains("not a member", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("only", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("cannot", StringComparison.OrdinalIgnoreCase))
            return StatusCode(403, new { Message = error });

        return BadRequest(new { Message = error });
    }
}