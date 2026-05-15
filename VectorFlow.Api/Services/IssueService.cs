using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VectorFlow.Api.Data;
using VectorFlow.Shared.DTOs;
using VectorFlow.Shared.Enums;
using VectorFlow.Api.Models;
using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Services;

public class IssueService(
    AppDbContext db,
    UserManager<AppUser> userManager,
    IProjectHubBroadcaster broadcaster) : IIssueService
{
    // ── Get issues ────────────────────────────────────────────────────────────

    public async Task<List<IssueDto>> GetIssuesAsync(
        Guid projectId,
        string requestingUserId,
        IssueStatus? status = null,
        IssuePriority? priority = null,
        IssueType? type = null,
        string? assigneeId = null)
    {
        if (!await IsMemberOfProjectWorkspaceAsync(projectId, requestingUserId))
            return [];

        var query = db.Issues
            .Where(i => i.ProjectId == projectId)
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
            .Include(i => i.Comments)
            .AsQueryable();

        if (status.HasValue) query = query.Where(i => i.Status == status.Value);
        if (priority.HasValue) query = query.Where(i => i.Priority == priority.Value);
        if (type.HasValue) query = query.Where(i => i.Type == type.Value);
        if (!string.IsNullOrEmpty(assigneeId)) query = query.Where(i => i.AssigneeId == assigneeId);

        return await query
            .OrderBy(i => i.Status)
            .ThenBy(i => i.Position)
            .Select(i => MapToDto(i))
            .ToListAsync();
    }

    // ── Get single issue ──────────────────────────────────────────────────────

    public async Task<IssueDto?> GetIssueAsync(Guid issueId, string requestingUserId)
    {
        var issue = await db.Issues
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.Id == issueId);

        if (issue is null) return null;

        if (!await IsMemberOfProjectWorkspaceAsync(issue.ProjectId, requestingUserId))
            return null;

        return MapToDto(issue);
    }

    // ── Get activity logs ─────────────────────────────────────────────────────

    public async Task<List<ActivityLogDto>> GetActivityLogsAsync(Guid issueId, string requestingUserId)
    {
        var issue = await db.Issues.FindAsync(issueId);
        if (issue is null) return [];

        if (!await IsMemberOfProjectWorkspaceAsync(issue.ProjectId, requestingUserId))
            return [];

        return await db.ActivityLogs
            .Where(a => a.IssueId == issueId)
            .Include(a => a.Actor)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new ActivityLogDto
            {
                Id = a.Id,
                ActorId = a.ActorId,
                ActorDisplayName = a.Actor.DisplayName,
                ActorAvatarUrl = a.Actor.AvatarUrl,
                Action = a.Action,
                FromValue = a.FromValue,
                ToValue = a.ToValue,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    // ── Create issue ──────────────────────────────────────────────────────────

    public async Task<IssueResult> CreateIssueAsync(
        Guid projectId, CreateIssueRequest request, string reporterId)
    {
        if (!await IsMemberOfProjectWorkspaceAsync(projectId, reporterId))
            return IssueResult.Failure("You are not a member of this workspace.");

        // Validate assignee is a workspace member if provided
        if (!string.IsNullOrEmpty(request.AssigneeId))
        {
            var project = await db.Projects.FindAsync(projectId);
            var assigneeIsMember = await db.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == project!.WorkspaceId && m.UserId == request.AssigneeId);

            if (!assigneeIsMember)
                return IssueResult.Failure("The assignee is not a member of this workspace.");
        }

        // Atomically increment the issue counter and generate the key.
        // ExecuteUpdateAsync updates directly in the DB without loading the entity,
        // avoiding race conditions from two users creating issues simultaneously.
        var updatedCount = await db.Projects
            .Where(p => p.Id == projectId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IssueCounter, p => p.IssueCounter + 1));

        if (updatedCount == 0)
            return IssueResult.Failure("Project not found.");

        var project2 = await db.Projects.FindAsync(projectId);
        var issueKey = $"{project2!.KeyPrefix}-{project2.IssueCounter}";

        // Resolve position — place new issue at the bottom of its column
        var maxPosition = await db.Issues
            .Where(i => i.ProjectId == projectId && i.Status == request.Status)
            .MaxAsync(i => (double?)i.Position) ?? 0;

        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Key = issueKey,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Status = request.Status,
            Priority = request.Priority,
            Type = request.Type,
            AssigneeId = string.IsNullOrEmpty(request.AssigneeId) ? null : request.AssigneeId,
            ReporterId = reporterId,
            // Ensure due date sent from server gets converted to UTC
            DueDate = request.DueDate.HasValue
                ? DateTime.SpecifyKind(request.DueDate.Value, DateTimeKind.Utc)
                : null,
            Position = maxPosition + 1000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await db.Issues.AddAsync(issue);

        // Attach labels
        if (request.LabelIds.Any())
        {
            var validLabelIds = await db.Labels
                .Where(l => l.ProjectId == projectId && request.LabelIds.Contains(l.Id))
                .Select(l => l.Id)
                .ToListAsync();

            foreach (var labelId in validLabelIds)
                await db.IssueLabels.AddAsync(new IssueLabel { IssueId = issue.Id, LabelId = labelId });
        }

        await db.SaveChangesAsync();

        var dto = await LoadIssueDtoAsync(issue.Id);
        var actor = await userManager.FindByIdAsync(reporterId);
        await broadcaster.BroadcastIssueCreatedAsync(projectId, dto, reporterId, actor?.DisplayName ?? string.Empty);

        return IssueResult.Success(dto);
    }

    // ── Update issue ──────────────────────────────────────────────────────────

    public async Task<IssueResult> UpdateIssueAsync(
        Guid issueId, UpdateIssueRequest request, string requestingUserId)
    {
        var issue = await db.Issues
            .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .FirstOrDefaultAsync(i => i.Id == issueId);

        if (issue is null) return IssueResult.Failure("Issue not found.");

        var (canEdit, errorMsg) = await CanEditIssueAsync(issue, requestingUserId);
        if (!canEdit) return IssueResult.Failure(errorMsg!);

        var activityLogs = new List<ActivityLog>();

        // Track and log each changed field individually
        if (request.Title != null && issue.Title != request.Title.Trim())
        {
            activityLogs.Add(BuildLog(issueId, requestingUserId, ActivityAction.TitleChanged,
                issue.Title, request.Title.Trim()));
            issue.Title = request.Title.Trim();
        }

        if (issue.Priority != request.Priority)
        {
            activityLogs.Add(BuildLog(issueId, requestingUserId, ActivityAction.PriorityChanged,
                issue.Priority.ToString(), request.Priority.ToString()));
            issue.Priority = request.Priority;
        }

        if (issue.Type != request.Type)
        { 
            issue.Type = request.Type;
            // Type changes are functional but not surfaced in the activity log
        }

        if (issue.AssigneeId != request.AssigneeId)
        {
            var newAssigneeName = request.AssigneeId is null
                ? "Unassigned"
                : (await db.Users.FindAsync(request.AssigneeId))?.DisplayName ?? request.AssigneeId;

            activityLogs.Add(BuildLog(issueId, requestingUserId, ActivityAction.AssigneeChanged,
                issue.Assignee?.DisplayName ?? "Unassigned", newAssigneeName));
            issue.AssigneeId = request.AssigneeId;
        }

        if (issue.DueDate != request.DueDate)
        {
            activityLogs.Add(BuildLog(issueId, requestingUserId, ActivityAction.DueDateChanged,
                issue.DueDate?.ToString("yyyy-MM-dd"), request.DueDate?.ToString("yyyy-MM-dd")));
            if (request.DueDate != null)
            {
                issue.DueDate = DateTime.SpecifyKind(request.DueDate.Value, DateTimeKind.Utc);
            }
            else
            {
                issue.DueDate = request.DueDate;
            }
        }

        if (request.Status is not null && issue.Status != request.Status)
        {
            activityLogs.Add(BuildLog(issueId, requestingUserId, ActivityAction.StatusChanged,
                issue.Status.ToString(), request.Status.ToString()));
            issue.Status = (IssueStatus)request.Status;
        }

        // Sync labels — diff the current set against the requested set
        var currentLabelIds = issue.IssueLabels.Select(il => il.LabelId).ToHashSet();
        var requestedLabelIds = request.LabelIds.ToHashSet();

        var toAdd = requestedLabelIds.Except(currentLabelIds).ToList();
        var toRemove = currentLabelIds.Except(requestedLabelIds).ToList();

        foreach (var labelId in toAdd)
        {
            var label = await db.Labels.FindAsync(labelId);
            if (label is null || label.ProjectId != issue.ProjectId) continue;

            await db.IssueLabels.AddAsync(new IssueLabel { IssueId = issueId, LabelId = labelId });
            activityLogs.Add(BuildLog(issueId, requestingUserId, ActivityAction.LabelAdded, null, label.Name));
        }

        foreach (var labelId in toRemove)
        {
            var issueLabel = issue.IssueLabels.First(il => il.LabelId == labelId);
            db.IssueLabels.Remove(issueLabel);
            activityLogs.Add(BuildLog(issueId, requestingUserId, ActivityAction.LabelRemoved,
                issueLabel.Label?.Name ?? labelId.ToString(), null));
        }

        issue.UpdatedAt = DateTime.UtcNow;

        if (activityLogs.Any())
            await db.ActivityLogs.AddRangeAsync(activityLogs);

        await db.SaveChangesAsync();

        var dto = await LoadIssueDtoAsync(issueId);
        var actor = await userManager.FindByIdAsync(requestingUserId);
        await broadcaster.BroadcastIssueUpdatedAsync(issue.ProjectId, dto, requestingUserId, actor?.DisplayName ?? string.Empty);

        return IssueResult.Success(dto);
    }

    // ── Update status ─────────────────────────────────────────────────────────

    public async Task<IssueResult> UpdateIssueStatusAsync(
        Guid issueId, UpdateIssueStatusRequest request, string requestingUserId)
    {
        var issue = await db.Issues
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.Id == issueId);

        if (issue is null) return IssueResult.Failure("Issue not found.");

        if (!await IsMemberOfProjectWorkspaceAsync(issue.ProjectId, requestingUserId))
            return IssueResult.Failure("You are not a member of this workspace.");

        if (issue.Status == request.Status)
            return IssueResult.Success(MapToDto(issue));

        var oldStatus = issue.Status.ToString();

        var maxPosition = await db.Issues
            .Where(i => i.ProjectId == issue.ProjectId && i.Status == request.Status)
            .MaxAsync(i => (double?)i.Position) ?? 0;

        var log = BuildLog(issueId, requestingUserId, ActivityAction.StatusChanged,
            oldStatus, request.Status.ToString());

        issue.Status = request.Status;
        issue.Position = maxPosition + 1000;
        issue.UpdatedAt = DateTime.UtcNow;

        await db.ActivityLogs.AddAsync(log);
        await db.SaveChangesAsync();

        var actor = await userManager.FindByIdAsync(requestingUserId);
        await broadcaster.BroadcastIssueStatusChangedAsync(
            issue.ProjectId, issueId, oldStatus, request.Status.ToString(),
            issue.Position, requestingUserId, actor?.DisplayName ?? string.Empty);

        return IssueResult.Success(MapToDto(issue));
    }

    // ── Update position ───────────────────────────────────────────────────────

    public async Task<IssueResult> UpdateIssuePositionAsync(
        Guid issueId, UpdateIssuePositionRequest request, string requestingUserId)
    {
        var issue = await db.Issues
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.Id == issueId);

        if (issue is null) return IssueResult.Failure("Issue not found.");

        if (!await IsMemberOfProjectWorkspaceAsync(issue.ProjectId, requestingUserId))
            return IssueResult.Failure("You are not a member of this workspace.");

        issue.Position = request.Position;
        issue.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await broadcaster.BroadcastIssuePositionChangedAsync(
            issue.ProjectId, issueId, request.Position, requestingUserId);

        return IssueResult.Success(MapToDto(issue));
    }

    // ── Delete issue ──────────────────────────────────────────────────────────

    public async Task<IssueResult> DeleteIssueAsync(Guid issueId, string requestingUserId)
    {
        var issue = await db.Issues
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.Id == issueId);

        if (issue is null) return IssueResult.Failure("Issue not found.");

        var (canEdit, errorMsg) = await CanEditIssueAsync(issue, requestingUserId);
        if (!canEdit) return IssueResult.Failure(errorMsg!);

        var dto = MapToDto(issue);
        var projectId = issue.ProjectId;

        db.Issues.Remove(issue);
        await db.SaveChangesAsync();

        await broadcaster.BroadcastIssueDeletedAsync(projectId, issueId, dto.Key, requestingUserId);

        return IssueResult.Success(dto);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Checks whether the user can edit or delete the issue.
    /// Allowed: the reporter, or any Owner/Admin in the workspace.
    /// </summary>
    private async Task<(bool canEdit, string? error)> CanEditIssueAsync(
        Issue issue, string requestingUserId)
    {
        if (!await IsMemberOfProjectWorkspaceAsync(issue.ProjectId, requestingUserId))
            return (false, "You are not a member of this workspace.");

        if (issue.ReporterId == requestingUserId)
            return (true, null);

        var project = await db.Projects.FindAsync(issue.ProjectId);
        var role = await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == project!.WorkspaceId && m.UserId == requestingUserId)
            .Select(m => (WorkspaceRole?)m.Role)
            .FirstOrDefaultAsync();

        if (role is WorkspaceRole.Owner or WorkspaceRole.Admin)
            return (true, null);

        return (false, "You can only edit issues you reported.");
    }

    private async Task<bool> IsMemberOfProjectWorkspaceAsync(Guid projectId, string userId)
    {
        var project = await db.Projects.FindAsync(projectId);
        if (project is null) return false;

        return await db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == project.WorkspaceId && m.UserId == userId);
    }

    /// <summary>
    /// Reloads the issue with all navigation properties after a write operation
    /// so the returned DTO has fully populated nested data.
    /// </summary>
    private async Task<IssueDto> LoadIssueDtoAsync(Guid issueId)
    {
        var issue = await db.Issues
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
            .Include(i => i.Comments)
            .FirstAsync(i => i.Id == issueId);

        return MapToDto(issue);
    }

    private static ActivityLog BuildLog(
        Guid issueId, string actorId, ActivityAction action,
        string? fromValue, string? toValue) => new()
        {
            Id = Guid.NewGuid(),
            IssueId = issueId,
            ActorId = actorId,
            Action = action,
            FromValue = fromValue,
            ToValue = toValue,
            CreatedAt = DateTime.UtcNow
        };

    private static IssueDto MapToDto(Issue issue) => new()
    {
        Id = issue.Id,
        ProjectId = issue.ProjectId,
        Key = issue.Key,
        Title = issue.Title,
        Description = issue.Description,
        Status = issue.Status,
        Priority = issue.Priority,
        Type = issue.Type,
        AssigneeId = issue.AssigneeId,
        AssigneeDisplayName = issue.Assignee?.DisplayName,
        AssigneeAvatarUrl = issue.Assignee?.AvatarUrl,
        ReporterId = issue.ReporterId,
        ReporterDisplayName = issue.Reporter?.DisplayName ?? string.Empty,
        DueDate = issue.DueDate,
        Position = issue.Position,
        CreatedAt = issue.CreatedAt,
        UpdatedAt = issue.UpdatedAt,
        CommentCount = issue.Comments.Count,
        Labels = issue.IssueLabels
            .Select(il => new LabelDto
            {
                Id = il.Label.Id,
                Name = il.Label.Name,
                Color = il.Label.Color
            }).ToList()
    };
}