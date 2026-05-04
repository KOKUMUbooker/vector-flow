namespace VectorFlow.Api.Models;

public class Project
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Short uppercase prefix used to generate issue keys.
    /// e.g. "VF" → issues become VF-1, VF-2, VF-3 ...
    /// Must be unique within a workspace. 2–6 characters recommended.
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Monotonically increasing counter for generating issue keys.
    /// Incremented atomically on every issue creation.
    /// Never reset — even if issues are deleted, keys are not reused.
    /// </summary>
    public int IssueCounter { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Workspace Workspace { get; set; } = null!;
    public ICollection<Issue> Issues { get; set; } = [];
    public ICollection<Label> Labels { get; set; } = [];
}