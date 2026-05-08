using Microsoft.EntityFrameworkCore;
using VectorFlow.Api.Data;
using VectorFlow.Shared.DTOs;
using VectorFlow.Shared.Enums;
using VectorFlow.Api.Models;
using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Services;

public class LabelService(AppDbContext db) : ILabelService
{
    // ── Get labels ────────────────────────────────────────────────────────────

    public async Task<List<LabelDto>> GetLabelsAsync(Guid projectId, string requestingUserId)
    {
        if (!await CanAccessProjectAsync(projectId, requestingUserId))
            return [];

        return await db.Labels
            .Where(l => l.ProjectId == projectId)
            .OrderBy(l => l.Name)
            .Select(l => MapToDto(l))
            .ToListAsync();
    }

    // ── Create label ──────────────────────────────────────────────────────────

    public async Task<LabelResult> CreateLabelAsync(
        Guid projectId, CreateLabelRequest request, string requestingUserId)
    {
        var role = await GetRoleForProjectAsync(projectId, requestingUserId);

        if (role is null)
            return LabelResult.Failure("You are not a member of this workspace.");

        if (role == WorkspaceRole.Member)
            return LabelResult.Failure("Only Owners and Admins can create labels.");

        // Label names must be unique within a project
        var nameTaken = await db.Labels.AnyAsync(l =>
            l.ProjectId == projectId &&
            l.Name.ToLower() == request.Name.Trim().ToLower());

        if (nameTaken)
            return LabelResult.Failure($"A label named '{request.Name.Trim()}' already exists in this project.");

        var label = new Label
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = request.Name.Trim(),
            Color = request.Color.ToUpperInvariant()
        };

        await db.Labels.AddAsync(label);
        await db.SaveChangesAsync();

        return LabelResult.Success(MapToDto(label));
    }

    // ── Update label ──────────────────────────────────────────────────────────

    public async Task<LabelResult> UpdateLabelAsync(
        Guid labelId, UpdateLabelRequest request, string requestingUserId)
    {
        var label = await db.Labels
            .FirstOrDefaultAsync(l => l.Id == labelId);

        if (label is null)
            return LabelResult.Failure("Label not found.");

        var role = await GetRoleForProjectAsync(label.ProjectId, requestingUserId);

        if (role is null)
            return LabelResult.Failure("You are not a member of this workspace.");

        if (role == WorkspaceRole.Member)
            return LabelResult.Failure("Only Owners and Admins can update labels.");

        // Check name uniqueness — exclude the current label from the check
        var nameTaken = await db.Labels.AnyAsync(l =>
            l.ProjectId == label.ProjectId &&
            l.Id != labelId &&
            l.Name.ToLower() == request.Name.Trim().ToLower());

        if (nameTaken)
            return LabelResult.Failure($"A label named '{request.Name.Trim()}' already exists in this project.");

        label.Name = request.Name.Trim();
        label.Color = request.Color.ToUpperInvariant();

        await db.SaveChangesAsync();

        return LabelResult.Success(MapToDto(label));
    }

    // ── Delete label ──────────────────────────────────────────────────────────

    public async Task<LabelResult> DeleteLabelAsync(Guid labelId, string requestingUserId)
    {
        var label = await db.Labels
            .FirstOrDefaultAsync(l => l.Id == labelId);

        if (label is null)
            return LabelResult.Failure("Label not found.");

        var role = await GetRoleForProjectAsync(label.ProjectId, requestingUserId);

        if (role is null)
            return LabelResult.Failure("You are not a member of this workspace.");

        if (role == WorkspaceRole.Member)
            return LabelResult.Failure("Only Owners and Admins can delete labels.");

        var dto = MapToDto(label);

        // IssueLabel rows are removed automatically via cascade configured
        // in IssueLabelConfiguration — no manual cleanup needed
        db.Labels.Remove(label);
        await db.SaveChangesAsync();

        return LabelResult.Success(dto);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<bool> CanAccessProjectAsync(Guid projectId, string userId)
    {
        var project = await db.Projects.FindAsync(projectId);
        if (project is null) return false;

        return await db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == project.WorkspaceId && m.UserId == userId);
    }

    private async Task<WorkspaceRole?> GetRoleForProjectAsync(Guid projectId, string userId)
    {
        var project = await db.Projects.FindAsync(projectId);
        if (project is null) return null;

        return await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == project.WorkspaceId && m.UserId == userId)
            .Select(m => (WorkspaceRole?)m.Role)
            .FirstOrDefaultAsync();
    }

    private static LabelDto MapToDto(Label label) => new()
    {
        Id = label.Id,
        Name = label.Name,
        Color = label.Color
    };
}