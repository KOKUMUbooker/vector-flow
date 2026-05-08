using VectorFlow.Shared.Enums;

namespace VectorFlow.Api.Models;

/// <summary>
/// The central entity of VectorFlow. Represents a unit of work within a project.
/// </summary>
public class Issue
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    /// <summary>
    /// Human-readable identifier. e.g. "VF-42".
    /// Built from Project.KeyPrefix + "-" + Project.IssueCounter.
    /// Unique within a project. Never changes after creation.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary>Markdown-formatted description. Null means no description yet.</summary>
    public string? Description { get; set; }

    public IssueStatus Status { get; set; } = IssueStatus.Backlog;

    public IssuePriority Priority { get; set; } = IssuePriority.None;

    public IssueType Type { get; set; } = IssueType.Task;

    /// <summary>
    /// The user this issue is assigned to. Null means unassigned.
    /// Uses SetNull on delete so removing a user doesn't cascade-delete their issues.
    /// </summary>
    public string? AssigneeId { get; set; }

    /// <summary>The user who created the issue. Never null after creation.</summary>
    public string ReporterId { get; set; } = string.Empty;

    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Float-based ordering within a Kanban column.
    /// Allows inserting between two cards without renumbering all positions.
    /// Default large value so new issues appear at the bottom of Backlog.
    /// </summary>
    public double Position { get; set; } = 1000;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Project Project { get; set; } = null!;
    public AppUser? Assignee { get; set; }
    public AppUser Reporter { get; set; } = null!;
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
    public ICollection<IssueLabel> IssueLabels { get; set; } = [];
}