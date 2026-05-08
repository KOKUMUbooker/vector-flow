using VectorFlow.Shared.DTOs;

namespace VectorFlow.Api.Services.Interfaces;

/// <summary>
/// Abstracts SignalR broadcasting behind an interface so services
/// don't take a direct dependency on IHubContext — easier to test
/// and swap out if the transport layer changes.
/// </summary>
public interface IProjectHubBroadcaster
{
    Task BroadcastIssueCreatedAsync(Guid projectId, IssueDto issue, string actorId, string actorDisplayName);
    Task BroadcastIssueUpdatedAsync(Guid projectId, IssueDto issue, string actorId, string actorDisplayName);
    Task BroadcastIssueDeletedAsync(Guid projectId, Guid issueId, string issueKey, string actorId);
    Task BroadcastIssueStatusChangedAsync(Guid projectId, Guid issueId, string oldStatus, string newStatus, double newPosition, string actorId, string actorDisplayName);
    Task BroadcastIssuePositionChangedAsync(Guid projectId, Guid issueId, double newPosition, string actorId);
    Task BroadcastCommentCreatedAsync(Guid projectId, CommentDto comment);
    Task BroadcastCommentUpdatedAsync(Guid projectId, CommentDto comment);
    Task BroadcastCommentDeletedAsync(Guid projectId, Guid issueId, Guid commentId, string actorId);
}