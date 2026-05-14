using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VectorFlow.Api.Data;
using VectorFlow.Shared.DTOs;
using VectorFlow.Shared.Enums;

namespace VectorFlow.Api.Controllers;

[ApiController]
[Route("api/dashboard/")]
[Authorize]
public class DashboardController(
    AppDbContext db) : ControllerBase
{
    private const int WorkspaceCap   = 5;
    private const int IssueBucketCap = 5;
    private const int ProjectCap     = 5;

    /// <summary>
    /// GET /api/dashboard
    ///
    /// Returns everything the dashboard needs in one call:
    ///   - User's workspaces (capped at 5)
    ///   - Issues assigned to the user, pre-grouped into filter buckets (capped at 5 each)
    ///   - Recent projects across all workspaces (capped at 5)
    ///   - Pending invitations for the user
    ///   - Rolled-up stats for the stat strip
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (userId is null)  return Unauthorized();

        // ── 1. Workspaces the user belongs to ────────────────────────────
        var memberships = await db.WorkspaceMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Workspace)
                .ThenInclude(w => w.Members)
            .Include(m => m.Workspace)
                .ThenInclude(w => w.Projects)
            .OrderByDescending(m => m.JoinedAt)
            .Take(WorkspaceCap)
            .ToListAsync();

        var workspaceDtos = memberships.Select(m => new DashboardWorkspaceDto
        {
            Id           = m.WorkspaceId,
            Name         = m.Workspace.Name,
            Slug         = m.Workspace.Slug,
            Role         = (WorkspaceRole)(int)m.Role,
            ProjectCount = m.Workspace.Projects.Count,
            MemberCount  = m.Workspace.Members.Count
        }).ToList();

        // Short-circuit if first-time user
        if (workspaceDtos.Count == 0) return Ok(new DashboardDto());

        var workspaceIds = memberships.Select(m => m.WorkspaceId).ToList();

        // ── 2. Projects across all user workspaces ───────────────────────
        // Build a lookup of workspaceId → (name, slug) for enriching issues
        var workspaceLookup = memberships.ToDictionary(
            m => m.WorkspaceId,
            m => (m.Workspace.Name, m.Workspace.Slug));

        var projects = await db.Projects
            .Where(p => workspaceIds.Contains(p.WorkspaceId))
            .Include(p => p.Issues.Where(i => i.Status != IssueStatus.Done))
            .ToListAsync();

        var recentProjects = projects
            .OrderByDescending(p => p.Issues.Count)
            .Take(ProjectCap)
            .Select(p => new DashboardProjectDto
            {
                Id             = p.Id,
                WorkspaceId    = p.WorkspaceId,
                WorkspaceName  = workspaceLookup.TryGetValue(p.WorkspaceId, out var ws) ? ws.Name : string.Empty,
                WorkspaceSlug  = workspaceLookup.TryGetValue(p.WorkspaceId, out var ws2) ? ws2.Slug : string.Empty,
                Name           = p.Name,
                KeyPrefix      = p.KeyPrefix,
                OpenIssueCount = p.Issues.Count
            }).ToList();

        // ── 3. Issues assigned to the user (all projects, status != Done) ─
        var projectIds = projects.Select(p => p.Id).ToList();

        var rawIssues = await db.Issues
            .Where(i =>
                projectIds.Contains(i.ProjectId) &&
                i.AssigneeId == userId &&
                i.Status != IssueStatus.Done)
            .Include(i => i.Project)
            .Include(i => i.Assignee)
            .OrderBy(i => i.DueDate == null ? 1 : 0) // nulls last
            .ThenBy(i => i.DueDate)
            .ThenByDescending(i => (int)i.Priority)
            .ToListAsync();

        // Map to shared DTO once, then build buckets from the in-memory list
        var issueDtos = rawIssues.Select(i =>
        {
            var slug = workspaceLookup.TryGetValue(i.Project.WorkspaceId, out var w) ? w.Slug : string.Empty;
            var isOverdue = i.DueDate.HasValue
                && i.DueDate.Value.Date < DateTime.UtcNow.Date
                && i.Status != IssueStatus.Done;

            return new DashboardIssueDto
            {
                Id                  = i.Id,
                ProjectId           = i.ProjectId,
                Key                 = i.Key,
                Title               = i.Title,
                ProjectName         = i.Project.Name,
                WorkspaceSlug       = slug,
                Status              = (IssueStatus)(int)i.Status,
                Priority            = (IssuePriority)(int)i.Priority,
                Type                = (IssueType)(int)i.Type,
                AssigneeDisplayName = i.Assignee?.DisplayName,
                DueDate             = i.DueDate,
                IsOverdue           = isOverdue
            };
        }).ToList();

        // Pre-group into buckets — UI switches tabs without re-fetching
        var issues = new DashboardIssuesDto
        {
            All = issueDtos
                .OrderBy(i => i.IsOverdue ? 0 : 1)
                .ThenByDescending(i => (int)i.Priority)
                .Take(IssueBucketCap)
                .ToList(),

            InProgress = issueDtos
                .Where(i => i.Status == IssueStatus.InProgress)
                .Take(IssueBucketCap)
                .ToList(),

            Overdue = issueDtos
                .Where(i => i.IsOverdue)
                .Take(IssueBucketCap)
                .ToList(),

            HighPriority = issueDtos
                .Where(i => i.Priority is IssuePriority.Urgent or IssuePriority.High)
                .Take(IssueBucketCap)
                .ToList()
        };

        // ── 4. Pending invitations ────────────────────────────────────────
        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        var invitations = await db.Invitations
            .Where(i =>
                i.InvitedEmail == userEmail.ToLowerInvariant() &&
                i.Status == InvitationStatus.Pending &&
                i.ExpiresAt > DateTime.UtcNow)
            .Include(i => i.Workspace)
            .Include(i => i.InvitedBy)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new DashboardInvitationDto
            {
                Id                   = i.Id,
                WorkspaceId          = i.WorkspaceId,
                WorkspaceName        = i.Workspace.Name,
                InvitedByDisplayName = i.InvitedBy.DisplayName,
                ExpiresAt            = i.ExpiresAt,
                Token                = i.Token,
            })
            .ToListAsync();

        // ── 5. Stats ──────────────────────────────────────────────────────
        // Compute from the full untruncated list for accuracy
        var stats = new DashboardStatsDto
        {
            AssignedCount   = issueDtos.Count,
            OverdueCount    = issueDtos.Count(i => i.IsOverdue),
            InProgressCount = issueDtos.Count(i => i.Status == IssueStatus.InProgress),
            WorkspaceCount  = memberships.Count
        };

        return Ok(new DashboardDto
        {
            Workspaces         = workspaceDtos,
            Issues             = issues,
            RecentProjects     = recentProjects,
            PendingInvitations = invitations,
            Stats              = stats
        });
    }

    
    [HttpGet("workspaces/{workspaceSlug}")]
    public async Task<IActionResult> GetWorkspaceDetails(string workspaceSlug) 
    {
        var workspaceExists = await db.Workspaces
            .Where(w => w.Slug == workspaceSlug)
            .Select(w => new {w.Id})
            .SingleOrDefaultAsync();
        
        if (workspaceExists is null) 
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null) return Unauthorized();

        var currentUserRole = await db.WorkspaceMembers
                .Where(m => m.UserId == userId && m.WorkspaceId == workspaceExists.Id)
                .Select(r => new { r.Role })
                .FirstOrDefaultAsync();

        if (currentUserRole is null) return Unauthorized();

        var workspaceData = await db.Workspaces
              .Where(w => w.Id == workspaceExists.Id)
              .Include(w => w.Projects)
              .Include(w => w.Members)
              .Include(w => w.Invitations)
              .Select(w => new { 
                Id = w.Id,
                Name = w.Name,
                Slug = w.Slug,
                OwnerId = w.OwnerId,
                Description = w.Description,
                Members = w.Members.Select(m => new WorkspaceMemberDto
                {
                    Email = m.User.Email ?? "",
                    AvatarUrl = m.User.AvatarUrl,
                    DisplayName = m.User.DisplayName,
                    IsOwner = m.UserId == w.OwnerId,
                    UserId = m.UserId,
                    JoinedAt = m.JoinedAt,
                    Role = m.Role
                }).ToList(),
                Projects = w.Projects.Select(p => new ProjectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    KeyPrefix = p.KeyPrefix,
                    IssueCount = p.Issues.Count,
                    IssueCounter = p.IssueCounter,
                    LabelCount = p.Labels.Count,
                    WorkspaceId = p.WorkspaceId,
                    CreatedAt = p.CreatedAt,
                }).ToList(),
                Invitations = w.Invitations.Select(i => new InvitationDto
                {
                    Id = i.Id,
                    CreatedAt = i.CreatedAt,
                    ExpiresAt = i.ExpiresAt,
                    InvitedByDisplayName = i.InvitedBy.DisplayName,
                    InvitedEmail = i.InvitedEmail,
                    Status = i.Status,
                    WorkspaceId = i.WorkspaceId,
                    WorkspaceName = i.Workspace.Name,
                    WorkspaceSlug = i.Workspace.Slug,
                    Token = i.Token,
                }).ToList(),
                CurrentUserRole = currentUserRole.Role,
                MemberCount = w.Members.Count,
                ProjectCount = w.Projects.Count,
                CreatedAt = w.CreatedAt,
              })
              .FirstOrDefaultAsync();

        if (workspaceData is null) return NotFound();

        var workspace = new WorkspaceDto
        {
            Id = workspaceData.Id,
            Name = workspaceData.Name,
            CreatedAt = workspaceData.CreatedAt,
            CurrentUserRole = currentUserRole.Role,
            Description = workspaceData.Description,
            MemberCount = workspaceData.MemberCount,
            OwnerId = workspaceData.OwnerId,
            ProjectCount = workspaceData.ProjectCount,
            Slug = workspaceData.Slug,
        };

        return Ok(new WorkspaceDetailsDashboardDto { 
            Workspace = workspace,
            Invitations = workspaceData.Invitations,
            Members = workspaceData.Members,
            Projects = workspaceData.Projects,
        });
    }
}