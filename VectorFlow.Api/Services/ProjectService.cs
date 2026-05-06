using Microsoft.EntityFrameworkCore;
using VectorFlow.Api.Data;
using VectorFlow.Api.DTOs;
using VectorFlow.Api.Enums;
using VectorFlow.Api.Models;
using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Services;

public class ProjectService(AppDbContext db) : IProjectService
{
    // ── Get all projects in a workspace ───────────────────────────────────────

    public async Task<List<ProjectDto>> GetProjectsAsync(Guid workspaceId, string requestingUserId)
    {
        // Verify the user is a workspace member
        var isMember = await db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == requestingUserId);

        if (!isMember) return [];

        return await db.Projects
            .Where(p => p.WorkspaceId == workspaceId)
            .Include(p => p.Issues)
            .Include(p => p.Labels)
            .OrderBy(p => p.CreatedAt)
            .Select(p => MapToDto(p))
            .ToListAsync();
    }

    // ── Get single project ────────────────────────────────────────────────────

    public async Task<ProjectDto?> GetProjectAsync(Guid projectId, string requestingUserId)
    {
        var project = await db.Projects
            .Include(p => p.Issues)
            .Include(p => p.Labels)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project is null) return null;

        // Verify the user is a member of the project's workspace
        var isMember = await db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == project.WorkspaceId && m.UserId == requestingUserId);

        return isMember ? MapToDto(project) : null;
    }

    // ── Create project ────────────────────────────────────────────────────────

    public async Task<ProjectResult> CreateProjectAsync(
        Guid workspaceId, CreateProjectRequest request, string requestingUserId)
    {
        var role = await GetUserRoleAsync(workspaceId, requestingUserId);

        if (role is null)
            return ProjectResult.Failure("You are not a member of this workspace.");

        if (role == WorkspaceRole.Member)
            return ProjectResult.Failure("Only Owners and Admins can create projects.");

        var keyPrefix = request.KeyPrefix.Trim().ToUpperInvariant();

        // KeyPrefix must be unique within the workspace
        var prefixTaken = await db.Projects
            .AnyAsync(p => p.WorkspaceId == workspaceId && p.KeyPrefix == keyPrefix);

        if (prefixTaken)
            return ProjectResult.Failure(
                $"The key prefix '{keyPrefix}' is already used by another project in this workspace.");

        var project = new Project
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            KeyPrefix = keyPrefix,
            IssueCounter = 0,
            CreatedAt = DateTime.UtcNow
        };

        await db.Projects.AddAsync(project);
        await db.SaveChangesAsync();

        // Reload with navigation properties for accurate counts
        var created = await db.Projects
            .Include(p => p.Issues)
            .Include(p => p.Labels)
            .FirstAsync(p => p.Id == project.Id);

        return ProjectResult.Success(MapToDto(created));
    }

    // ── Update project ────────────────────────────────────────────────────────

    public async Task<ProjectResult> UpdateProjectAsync(
        Guid projectId, UpdateProjectRequest request, string requestingUserId)
    {
        var project = await db.Projects
            .Include(p => p.Issues)
            .Include(p => p.Labels)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project is null)
            return ProjectResult.Failure("Project not found.");

        var role = await GetUserRoleAsync(project.WorkspaceId, requestingUserId);

        if (role is null)
            return ProjectResult.Failure("You are not a member of this workspace.");

        if (role == WorkspaceRole.Member)
            return ProjectResult.Failure("Only Owners and Admins can update projects.");

        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        // KeyPrefix is intentionally not updatable — changing it would
        // invalidate all existing issue keys (VF-1, VF-2 etc.)

        await db.SaveChangesAsync();

        return ProjectResult.Success(MapToDto(project));
    }

    // ── Delete project ────────────────────────────────────────────────────────

    public async Task<ProjectResult> DeleteProjectAsync(Guid projectId, string requestingUserId)
    {
        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project is null)
            return ProjectResult.Failure("Project not found.");

        var role = await GetUserRoleAsync(project.WorkspaceId, requestingUserId);

        if (role is null)
            return ProjectResult.Failure("You are not a member of this workspace.");

        if (role == WorkspaceRole.Member)
            return ProjectResult.Failure("Only Owners and Admins can delete projects.");

        // EF cascade handles issues → comments, activity logs, labels
        db.Projects.Remove(project);
        await db.SaveChangesAsync();

        return ProjectResult.Success(MapToDto(project));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<WorkspaceRole?> GetUserRoleAsync(Guid workspaceId, string userId)
    {
        var member = await db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

        return member?.Role;
    }

    private static ProjectDto MapToDto(Project project) => new()
    {
        Id = project.Id,
        WorkspaceId = project.WorkspaceId,
        Name = project.Name,
        Description = project.Description,
        KeyPrefix = project.KeyPrefix,
        IssueCounter = project.IssueCounter,
        CreatedAt = project.CreatedAt,
        IssueCount = project.Issues.Count,
        LabelCount = project.Labels.Count
    };
}