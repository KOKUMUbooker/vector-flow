using Microsoft.AspNetCore.SignalR;
using VectorFlow.Api.DTOs;
using VectorFlow.Api.Hubs;
using VectorFlow.Shared.SignalR;
using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Services;

public class ProjectHubBroadcaster(IHubContext<ProjectHub> hubContext) : IProjectHubBroadcaster
{
    // ── Issues ────────────────────────────────────────────────────────────────

    public Task BroadcastIssueCreatedAsync(
        Guid projectId, IssueDto issue, string actorId, string actorDisplayName) =>
        hubContext.Clients
            .Group(ProjectHub.GroupKey(projectId))
            .SendAsync(HubEvents.IssueCreated, new IssueCreatedEvent
            {
                ProjectId = projectId,
                IssueId = issue.Id,
                Key = issue.Key,
                Title = issue.Title,
                Status = issue.Status.ToString(),
                Priority = issue.Priority.ToString(),
                Type = issue.Type.ToString(),
                AssigneeId = issue.AssigneeId,
                AssigneeDisplayName = issue.AssigneeDisplayName,
                Position = issue.Position,
                ActorId = actorId,
                ActorDisplayName = actorDisplayName
            });

    public Task BroadcastIssueUpdatedAsync(
        Guid projectId, IssueDto issue, string actorId, string actorDisplayName) =>
        hubContext.Clients
            .Group(ProjectHub.GroupKey(projectId))
            .SendAsync(HubEvents.IssueUpdated, new IssueUpdatedEvent
            {
                ProjectId = projectId,
                IssueId = issue.Id,
                Key = issue.Key,
                Title = issue.Title,
                Priority = issue.Priority.ToString(),
                Type = issue.Type.ToString(),
                AssigneeId = issue.AssigneeId,
                AssigneeDisplayName = issue.AssigneeDisplayName,
                DueDate = issue.DueDate,
                ActorId = actorId,
                ActorDisplayName = actorDisplayName,
                UpdatedAt = issue.UpdatedAt
            });

    public Task BroadcastIssueDeletedAsync(
        Guid projectId, Guid issueId, string issueKey, string actorId) =>
        hubContext.Clients
            .Group(ProjectHub.GroupKey(projectId))
            .SendAsync(HubEvents.IssueDeleted, new IssueDeletedEvent
            {
                ProjectId = projectId,
                IssueId = issueId,
                Key = issueKey,
                ActorId = actorId
            });

    public Task BroadcastIssueStatusChangedAsync(
        Guid projectId, Guid issueId, string oldStatus, string newStatus,
        double newPosition, string actorId, string actorDisplayName) =>
        hubContext.Clients
            .Group(ProjectHub.GroupKey(projectId))
            .SendAsync(HubEvents.IssueStatusChanged, new IssueStatusChangedEvent
            {
                ProjectId = projectId,
                IssueId = issueId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                NewPosition = newPosition,
                ActorId = actorId,
                ActorDisplayName = actorDisplayName
            });

    public Task BroadcastIssuePositionChangedAsync(
        Guid projectId, Guid issueId, double newPosition, string actorId) =>
        hubContext.Clients
            .Group(ProjectHub.GroupKey(projectId))
            .SendAsync(HubEvents.IssuePositionChanged, new IssuePositionChangedEvent
            {
                ProjectId = projectId,
                IssueId = issueId,
                NewPosition = newPosition,
                ActorId = actorId
            });

    // ── Comments ──────────────────────────────────────────────────────────────

    public Task BroadcastCommentCreatedAsync(Guid projectId, CommentDto comment) =>
        hubContext.Clients
            .Group(ProjectHub.GroupKey(projectId))
            .SendAsync(HubEvents.CommentCreated, new CommentCreatedEvent
            {
                ProjectId = projectId,
                IssueId = comment.IssueId,
                CommentId = comment.Id,
                AuthorId = comment.AuthorId,
                AuthorDisplayName = comment.AuthorDisplayName,
                AuthorAvatarUrl = comment.AuthorAvatarUrl,
                Body = comment.Body,
                CreatedAt = comment.CreatedAt
            });

    public Task BroadcastCommentUpdatedAsync(Guid projectId, CommentDto comment) =>
        hubContext.Clients
            .Group(ProjectHub.GroupKey(projectId))
            .SendAsync(HubEvents.CommentUpdated, new CommentUpdatedEvent
            {
                ProjectId = projectId,
                IssueId = comment.IssueId,
                CommentId = comment.Id,
                Body = comment.Body,
                UpdatedAt = comment.UpdatedAt
            });

    public Task BroadcastCommentDeletedAsync(
        Guid projectId, Guid issueId, Guid commentId, string actorId) =>
        hubContext.Clients
            .Group(ProjectHub.GroupKey(projectId))
            .SendAsync(HubEvents.CommentDeleted, new CommentDeletedEvent
            {
                ProjectId = projectId,
                IssueId = issueId,
                CommentId = commentId,
                ActorId = actorId
            });
}