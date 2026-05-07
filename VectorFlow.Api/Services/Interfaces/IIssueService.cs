using VectorFlow.Api.DTOs;
using VectorFlow.Api.Enums;

namespace VectorFlow.Api.Services.Interfaces;

public interface IIssueService
{
    /// <summary>
    /// Returns all issues in a project, optionally filtered.
    /// Any workspace member can view.
    /// </summary>
    Task<List<IssueDto>> GetIssuesAsync(
        Guid projectId,
        string requestingUserId,
        IssueStatus? status = null,
        IssuePriority? priority = null,
        IssueType? type = null,
        string? assigneeId = null);

    /// <summary>
    /// Returns a single issue by ID with full detail.
    /// Any workspace member can view.
    /// </summary>
    Task<IssueDto?> GetIssueAsync(Guid issueId, string requestingUserId);

    /// <summary>
    /// Returns the activity log for an issue ordered chronologically.
    /// Any workspace member can view.
    /// </summary>
    Task<List<ActivityLogDto>> GetActivityLogsAsync(Guid issueId, string requestingUserId);

    /// <summary>
    /// Creates a new issue. Any workspace member can create.
    /// Atomically increments the project's IssueCounter to generate the key.
    /// </summary>
    Task<IssueResult> CreateIssueAsync(Guid projectId, CreateIssueRequest request, string reporterId);

    /// <summary>
    /// Updates issue fields. Reporter, Owner, and Admin only.
    /// Auto-logs a separate ActivityLog entry for every changed field.
    /// </summary>
    Task<IssueResult> UpdateIssueAsync(Guid issueId, UpdateIssueRequest request, string requestingUserId);

    /// <summary>
    /// Updates the status only — called by the Kanban drag-and-drop.
    /// Any workspace member can update status.
    /// Auto-logs a StatusChanged activity entry.
    /// </summary>
    Task<IssueResult> UpdateIssueStatusAsync(Guid issueId, UpdateIssueStatusRequest request, string requestingUserId);

    /// <summary>
    /// Updates the position within a Kanban column for card ordering.
    /// Any workspace member can reorder.
    /// Position changes are not logged in the activity log.
    /// </summary>
    Task<IssueResult> UpdateIssuePositionAsync(Guid issueId, UpdateIssuePositionRequest request, string requestingUserId);

    /// <summary>
    /// Deletes an issue. Reporter, Owner, and Admin only.
    /// </summary>
    Task<IssueResult> DeleteIssueAsync(Guid issueId, string requestingUserId);
}