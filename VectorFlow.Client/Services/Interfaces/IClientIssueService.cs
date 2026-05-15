namespace VectorFlow.Client.Services.Interfaces;

using VectorFlow.Shared.DTOs;
using VectorFlow.Shared.Enums;

public interface IClientIssueService
{
    /// <summary> Returns all issues in a project, optionally filtered </summary>
    Task<ServiceResult<List<IssueDto>>> GetIssuesAsync(
        Guid projectId,
        string requestingUserId,
        IssueStatus? status = null,
        IssuePriority? priority = null,
        IssueType? type = null,
        string? assigneeId = null);

    /// <summary> Returns a single issue by ID with full detail. </summary>
    Task<ServiceResult<IssueDto?>> GetIssueAsync(Guid projectId, Guid issueId);

    /// <summary> Returns the activity log for an issue ordered chronologically. </summary>
    Task<ServiceResult<List<ActivityLogDto>>> GetActivityLogsAsync(Guid projectId, Guid issueId);

    /// <summary> Creates a new issue. </summary>
    Task<ServiceResult<IssueDto?>> CreateIssueAsync(Guid projectId, CreateIssueRequest request);

    /// <summary> Updates issue fields. </summary>
    Task<ServiceResult<IssueDto>> UpdateIssueAsync(Guid projectId, Guid issueId, UpdateIssueRequest request);

    /// <summary> Updates the status only — called by the Kanban drag-and-drop. </summary>
    Task<ServiceResult<IssueDto>> UpdateIssueStatusAsync(Guid projectId, Guid issueId, UpdateIssueStatusRequest request);
    // <summary> Updates only the assignee </summary>
    Task<ServiceResult<IssueDto>> UpdateIssueAssigneeAsync(Guid projectId, Guid issueId, UpdateIssueAssigneeRequest request);

    // <summary> Updates only the DueDate </summary>
    Task<ServiceResult<IssueDto>> UpdateIssueDueDateAsync(Guid projectId, Guid issueId, UpdateIssueDueDateRequest request);

    /// <summary> Updates the position within a Kanban column for card ordering. </summary>
    Task<ServiceResult<IssueDto>> UpdateIssuePositionAsync(Guid projectId, Guid issueId, UpdateIssuePositionRequest request);

    /// <summary> Deletes an issue. </summary>
    Task<ServiceResult> DeleteIssueAsync(Guid projectId, Guid issueId);
}