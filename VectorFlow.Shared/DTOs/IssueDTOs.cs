using System.ComponentModel.DataAnnotations;
using VectorFlow.Shared.Enums;

namespace VectorFlow.Shared.DTOs;

// ── Requests ──────────────────────────────────────────────────────────────────

public class CreateIssueRequest
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public IssueStatus Status { get; set; } = IssueStatus.Backlog;
    public IssuePriority Priority { get; set; } = IssuePriority.None;
    public IssueType Type { get; set; } = IssueType.Task;

    public string? AssigneeId { get; set; }
    public DateTime? DueDate { get; set; }
    public List<Guid> LabelIds { get; set; } = [];
}

public class UpdateIssueRequest
{
    public string? Title { get; set; } = string.Empty;

    public string? Description { get; set; }
    public IssuePriority? Priority { get; set; } 
    public IssueType? Type { get; set; }
    public DateTime? DueDate { get; set; }
    public List<Guid> LabelIds { get; set; } = [];
}

/// <summary>
/// Separate request for status changes — used by the Kanban board drag-and-drop.
/// Keeping it separate means the board only sends one field instead of the full issue.
/// </summary>
public class UpdateIssueStatusRequest
{
    [Required]
    public IssueStatus Status { get; set; }
}

/// <summary>
/// Separate request for assignee changes — allows for assigning and unassigning an issue to users
/// </summary>
public class UpdateIssueAssigneeRequest
{
    public string? AssigneeId { get; set; }
}

/// <summary>
/// Separate request for position updates within a Kanban column.
/// Called when a card is reordered within the same column.
/// </summary>
public class UpdateIssuePositionRequest
{
    [Required]
    public double Position { get; set; }
}

// ── Responses ─────────────────────────────────────────────────────────────────

public class IssueDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IssueStatus Status { get; set; }
    public IssuePriority Priority { get; set; }
    public IssueType Type { get; set; }
    public string? AssigneeId { get; set; }
    public string? AssigneeDisplayName { get; set; }
    public string? AssigneeAvatarUrl { get; set; }
    public string ReporterId { get; set; } = string.Empty;
    public string ReporterDisplayName { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public double Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<LabelDto> Labels { get; set; } = [];
    public int CommentCount { get; set; }
}

public class LabelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class ActivityLogDto
{
    public Guid Id { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string ActorDisplayName { get; set; } = string.Empty;
    public string? ActorAvatarUrl { get; set; }
    public ActivityAction Action { get; set; }
    public string? FromValue { get; set; }
    public string? ToValue { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Service result ────────────────────────────────────────────────────────────

public class IssueResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public IssueDto? Issue { get; set; }

    public static IssueResult Success(IssueDto issue) =>
        new() { Succeeded = true, Issue = issue };

    public static IssueResult Failure(string error) =>
        new() { Error = error };
}
