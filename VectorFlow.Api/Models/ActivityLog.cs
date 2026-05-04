using VectorFlow.Api.Enums;

namespace VectorFlow.Api.Models;

/// <summary>
/// Append-only audit trail of all changes made to an issue.
/// Never updated or deleted — only inserted.
/// </summary>
public class ActivityLog
{
    public Guid Id { get; set; }

    public Guid IssueId { get; set; }

    /// <summary>The user who made the change.</summary>
    public string ActorId { get; set; } = string.Empty;

    public ActivityAction Action { get; set; }

    /// <summary>
    /// The previous value before the change. Stored as a string for simplicity.
    /// e.g. for StatusChanged: "Backlog", for AssigneeChanged: "john@example.com"
    /// Null for creation events where there is no previous value.
    /// </summary>
    public string? FromValue { get; set; }

    /// <summary>
    /// The new value after the change.
    /// e.g. for StatusChanged: "InProgress", for PriorityChanged: "High"
    /// </summary>
    public string? ToValue { get; set; }

    /// <summary>Immutable — activity logs are never edited.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Issue Issue { get; set; } = null!;
    public AppUser Actor { get; set; } = null!;
}