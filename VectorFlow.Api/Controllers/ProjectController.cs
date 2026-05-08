using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VectorFlow.Shared.DTOs;
using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceId:guid}/projects")]
[Authorize]
public class ProjectsController(IProjectService projectService) : ControllerBase
{
    // ── GET /api/workspaces/{workspaceId}/projects ────────────────────────────
    // Returns all projects in a workspace.
    // Any workspace member can view.

    [HttpGet]
    public async Task<IActionResult> GetProjects(Guid workspaceId)
    {
        var userId = GetUserId();
        var projects = await projectService.GetProjectsAsync(workspaceId, userId);
        return Ok(projects);
    }

    // ── GET /api/workspaces/{workspaceId}/projects/{projectId} ────────────────
    // Returns a single project.
    // Returns 404 for non-members — does not reveal the project exists.

    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> GetProject(Guid workspaceId, Guid projectId)
    {
        var userId = GetUserId();
        var project = await projectService.GetProjectAsync(projectId, userId);
        return project is null ? NotFound() : Ok(project);
    }

    // ── POST /api/workspaces/{workspaceId}/projects ───────────────────────────
    // Creates a new project. Owner/Admin only.

    [HttpPost]
    public async Task<IActionResult> CreateProject(
        Guid workspaceId, CreateProjectRequest request)
    {
        var userId = GetUserId();
        var result = await projectService.CreateProjectAsync(workspaceId, request, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return CreatedAtAction(
            nameof(GetProject),
            new { workspaceId, projectId = result.Project!.Id },
            result.Project);
    }

    // ── PUT /api/workspaces/{workspaceId}/projects/{projectId} ────────────────
    // Updates project name and description. Owner/Admin only.
    // KeyPrefix cannot be changed — use a separate endpoint if needed later.

    [HttpPut("{projectId:guid}")]
    public async Task<IActionResult> UpdateProject(
        Guid workspaceId, Guid projectId, UpdateProjectRequest request)
    {
        var userId = GetUserId();
        var result = await projectService.UpdateProjectAsync(projectId, request, userId);

        if (!result.Succeeded)
            return ToErrorResponse(result.Error!);

        return Ok(result.Project);
    }

    // ── DELETE /api/workspaces/{workspaceId}/projects/{projectId} ─────────────
    // Permanently deletes the project and all its data. Owner/Admin only.

    [HttpDelete("{projectId:guid}")]
    public async Task<IActionResult> DeleteProject(Guid workspaceId, Guid projectId)
    {
        var userId = GetUserId();
        var result = await projectService.DeleteProjectAsync(projectId, userId);

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