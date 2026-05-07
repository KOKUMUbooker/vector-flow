namespace VectorFlow.Shared.SignalR;

// ── Hub method names — used by both server and client ─────────────────────────
// Centralising these as constants prevents typos causing silent failures
// where the server broadcasts "IssueUpdated" but the client listens for "issueUpdated".

public static class HubEvents
{
    // Server → Client broadcasts
    public const string IssueCreated = "IssueCreated";
    public const string IssueUpdated = "IssueUpdated";
    public const string IssueDeleted = "IssueDeleted";
    public const string IssueStatusChanged = "IssueStatusChanged";
    public const string IssuePositionChanged = "IssuePositionChanged";

    public const string CommentCreated = "CommentCreated";
    public const string CommentUpdated = "CommentUpdated";
    public const string CommentDeleted = "CommentDeleted";

    // Client → Server calls
    public const string JoinProject = "JoinProject";
    public const string LeaveProject = "LeaveProject";
}

// ── Payloads broadcast to clients ─────────────────────────────────────────────
// Kept deliberately thin — only what the client needs to update its UI state.
// The client can always fetch full details via the REST API if needed.

public class IssueCreatedEvent
{
    public Guid ProjectId { get; set; }
    public Guid IssueId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? AssigneeId { get; set; }
    public string? AssigneeDisplayName { get; set; }
    public double Position { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string ActorDisplayName { get; set; } = string.Empty;
}

public class IssueUpdatedEvent
{
    public Guid ProjectId { get; set; }
    public Guid IssueId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? AssigneeId { get; set; }
    public string? AssigneeDisplayName { get; set; }
    public DateTime? DueDate { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string ActorDisplayName { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class IssueDeletedEvent
{
    public Guid ProjectId { get; set; }
    public Guid IssueId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
}

public class IssueStatusChangedEvent
{
    public Guid ProjectId { get; set; }
    public Guid IssueId { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public double NewPosition { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string ActorDisplayName { get; set; } = string.Empty;
}

public class IssuePositionChangedEvent
{
    public Guid ProjectId { get; set; }
    public Guid IssueId { get; set; }
    public double NewPosition { get; set; }
    public string ActorId { get; set; } = string.Empty;
}

public class CommentCreatedEvent
{
    public Guid ProjectId { get; set; }
    public Guid IssueId { get; set; }
    public Guid CommentId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CommentUpdatedEvent
{
    public Guid ProjectId { get; set; }
    public Guid IssueId { get; set; }
    public Guid CommentId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class CommentDeletedEvent
{
    public Guid ProjectId { get; set; }
    public Guid IssueId { get; set; }
    public Guid CommentId { get; set; }
    public string ActorId { get; set; } = string.Empty;
}