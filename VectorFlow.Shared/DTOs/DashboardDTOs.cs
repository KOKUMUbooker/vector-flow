using VectorFlow.Shared.Enums;

namespace VectorFlow.Shared.DTOs;

/// <summary>
/// Single aggregated response for GET /api/dashboard.
/// The backend does all the heavy lifting so the UI makes one call
/// and renders — no N+1 requests from the client.
/// </summary>
public class DashboardDto
{
    /// <summary>Workspaces the user belongs to, capped at 5, ordered by most recently active.</summary>
    public List<DashboardWorkspaceDto> Workspaces { get; set; } = [];

    /// <summary>Issues assigned to the user across all workspaces, grouped by filter type.</summary>
    public DashboardIssuesDto Issues { get; set; } = new();

    /// <summary>Up to 5 most active projects across all workspaces.</summary>
    public List<DashboardProjectDto> RecentProjects { get; set; } = [];

    /// <summary>Pending invitations for the current user.</summary>
    public List<DashboardInvitationDto> PendingInvitations { get; set; } = [];

    /// <summary>Rolled-up counts for the stat strip.</summary>
    public DashboardStatsDto Stats { get; set; } = new();

    /// <summary>True when the user has no workspaces — UI shows the empty/first-time state.</summary>
    public bool IsFirstTimeUser => !Workspaces.Any();
}

public class WorkspaceDetailsDashboardDto
{
    public WorkspaceDto Workspace { get; set; } = default!;

    public List<ProjectDto> Projects { get; set; } = [];

    public List<WorkspaceMemberDto> Members { get; set; } = [];

    public List<InvitationDto> Invitations { get; set; } = [];
}

public class DashboardStatsDto
{
    public int AssignedCount   { get; set; }
    public int OverdueCount    { get; set; }
    public int InProgressCount { get; set; }
    public int WorkspaceCount  { get; set; }
}

public class DashboardWorkspaceDto
{
    public Guid          Id           { get; set; }
    public string        Name         { get; set; } = string.Empty;
    public string        Slug         { get; set; } = string.Empty;
    public WorkspaceRole Role         { get; set; }
    public int           ProjectCount { get; set; }
    public int           MemberCount  { get; set; }
}

public class DashboardProjectDto
{
    public Guid   Id            { get; set; }
    public Guid   WorkspaceId   { get; set; }
    public string WorkspaceName { get; set; } = string.Empty;
    public string WorkspaceSlug { get; set; } = string.Empty;
    public string Name          { get; set; } = string.Empty;
    public string KeyPrefix     { get; set; } = string.Empty;
    public int    OpenIssueCount { get; set; }
}

/// <summary>
/// Issues pre-grouped server-side so the UI can switch between
/// filter tabs without re-fetching or re-filtering a large list.
/// Each bucket is capped at 5.
/// </summary>
public class DashboardIssuesDto
{
    public List<DashboardIssueDto> All         { get; set; } = [];
    public List<DashboardIssueDto> InProgress  { get; set; } = [];
    public List<DashboardIssueDto> Overdue     { get; set; } = [];
    public List<DashboardIssueDto> HighPriority { get; set; } = [];
}

public class DashboardIssueDto
{
    public Guid          Id           { get; set; }
    public Guid          ProjectId    { get; set; }
    public string        Key          { get; set; } = string.Empty;
    public string        Title        { get; set; } = string.Empty;
    public string        ProjectName  { get; set; } = string.Empty;
    public string        WorkspaceSlug { get; set; } = string.Empty;
    public IssueStatus   Status       { get; set; }
    public IssuePriority Priority     { get; set; }
    public IssueType     Type         { get; set; }
    public string?       AssigneeDisplayName { get; set; }
    public DateTime?     DueDate      { get; set; }
    public bool          IsOverdue    { get; set; }
}

public class DashboardInvitationDto
{
    public Guid   Id                   { get; set; }
    public Guid   WorkspaceId          { get; set; }
    public string WorkspaceName        { get; set; } = string.Empty;
    public string InvitedByDisplayName { get; set; } = string.Empty;
    public DateTime ExpiresAt          { get; set; }
    public string Token                 { get; set; } = string.Empty;
}

public class DashboardProjectData
{
    public required ProjectDto Project { get; set; }
    public List<WorkspaceMemberDto> Members { get; set; } = new List<WorkspaceMemberDto>();
    public List<LabelDto> Labels { get; set; } = new List<LabelDto>();
    public List<IssueDto> Issues { get; set; } = new List<IssueDto>();

}

public class DashboardFetchedIssueData
{
    public IssueDto Issue { get; set; } = new();
    public List<WorkspaceMemberDto> Members { get; set; } = new();
    public List<ActivityLogDto> ActivityLogs { get; set; } = new();
    public List<CommentDto> Comments { get; set; } = new();
    public string WorkspaceName { get; set; } = string.Empty;
    public List<LabelDto> Labels { get; set; } = new List<LabelDto>();
    public string ProjectName { get; set; } = string.Empty;
    public virtual WorkspaceRole? UserWorkspaceRole { get; set; }
}

public class DashboardIssueData : DashboardFetchedIssueData
{
    public override required WorkspaceRole? UserWorkspaceRole { get; set; }
}