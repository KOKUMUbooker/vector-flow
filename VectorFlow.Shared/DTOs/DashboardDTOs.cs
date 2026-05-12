using VectorFlow.Shared.Enums;

namespace VectorFlow.Shared.DTOs;


/// <summary>
/// Single aggregated response for GET /api/dashboard.
/// Gives the client everything the dashboard needs in one round-trip:
/// the current user's workspaces, their assigned open issues across all
/// projects, and a flattened list of recent projects for the jump-list.
/// </summary>
public class DashboardSummaryDto
{
    /// <summary>
    /// All workspaces the authenticated user belongs to,
    /// ordered by most recently created first.
    /// </summary>
    public List<WorkspaceDto> Workspaces { get; set; } = [];

    /// <summary>
    /// Issues assigned to the authenticated user across every project
    /// in every workspace, excluding Done issues.
    /// Each item is enriched with the project name and workspace slug
    /// so the UI can display them without extra look-ups.
    /// </summary>
    public List<DashboardIssueDto> AssignedIssues { get; set; } = [];

    /// <summary>
    /// Flattened list of all projects the user has access to,
    /// ordered by open issue count descending and capped at 10.
    /// Used for the "Recent projects" jump-list in the sidebar.
    /// </summary>
    public List<DashboardProjectDto> RecentProjects { get; set; } = [];
}

/// <summary>
/// A slim issue row enriched with workspace + project context,
/// since <see cref="IssueDto"/> only carries <c>ProjectId</c>.
/// </summary>
public class DashboardIssueDto
{
    // ── Core issue fields (mirrors IssueDto) ───────────────────────────
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public IssueStatus Status { get; set; }
    public IssuePriority Priority { get; set; }
    public IssueType Type { get; set; }
    public string? AssigneeId { get; set; }
    public string? AssigneeDisplayName { get; set; }
    public DateTime? DueDate { get; set; }

    // ── Context injected by the aggregation endpoint ───────────────────
    /// <summary>Display name of the project this issue belongs to.</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>Slug of the workspace that owns the project.</summary>
    public string WorkspaceSlug { get; set; } = string.Empty;
}

/// <summary>
/// A slim project row enriched with workspace context for the sidebar.
/// </summary>
public class DashboardProjectDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string WorkspaceSlug { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Total open (non-Done) issue count for this project.
    /// Sourced from <see cref="ProjectDto.IssueCount"/>.
    /// </summary>
    public int OpenIssueCount { get; set; }
}