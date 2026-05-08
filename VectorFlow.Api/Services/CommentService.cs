using Microsoft.EntityFrameworkCore;
using VectorFlow.Api.Data;
using VectorFlow.Shared.DTOs;
using VectorFlow.Api.Enums;
using VectorFlow.Api.Models;
using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Services;

public class CommentService(
    AppDbContext db,
    IProjectHubBroadcaster broadcaster) : ICommentService
{
    // ── Get comments ──────────────────────────────────────────────────────────

    public async Task<List<CommentDto>> GetCommentsAsync(Guid issueId, string requestingUserId)
    {
        if (!await CanAccessIssueAsync(issueId, requestingUserId))
            return [];

        return await db.Comments
            .Where(c => c.IssueId == issueId)
            .Include(c => c.Author)
            .OrderBy(c => c.CreatedAt)
            .Select(c => MapToDto(c))
            .ToListAsync();
    }

    // ── Create comment ────────────────────────────────────────────────────────

    public async Task<CommentResult> CreateCommentAsync(
        Guid issueId, CreateCommentRequest request, string authorId)
    {
        if (!await CanAccessIssueAsync(issueId, authorId))
            return CommentResult.Failure("You are not a member of this workspace.");

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            IssueId = issueId,
            AuthorId = authorId,
            Body = request.Body.Trim(),
            IsEdited = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await db.Comments.AddAsync(comment);
        await db.SaveChangesAsync();

        // Reload to populate Author navigation property for the response
        await db.Entry(comment).Reference(c => c.Author).LoadAsync();

        var dto = MapToDto(comment);
        var projectId = await GetProjectIdForIssueAsync(issueId);
        await broadcaster.BroadcastCommentCreatedAsync(projectId, dto);

        return CommentResult.Success(dto);
    }

    // ── Update comment ────────────────────────────────────────────────────────

    public async Task<CommentResult> UpdateCommentAsync(
        Guid commentId, UpdateCommentRequest request, string requestingUserId)
    {
        var comment = await db.Comments
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment is null)
            return CommentResult.Failure("Comment not found.");

        // Only the author can edit their own comment
        if (comment.AuthorId != requestingUserId)
            return CommentResult.Failure("You can only edit your own comments.");

        if (!await CanAccessIssueAsync(comment.IssueId, requestingUserId))
            return CommentResult.Failure("You are not a member of this workspace.");

        comment.Body = request.Body.Trim();
        comment.IsEdited = true;
        comment.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var dto = MapToDto(comment);
        var projectId = await GetProjectIdForIssueAsync(comment.IssueId);
        await broadcaster.BroadcastCommentUpdatedAsync(projectId, dto);

        return CommentResult.Success(dto);
    }

    // ── Delete comment ────────────────────────────────────────────────────────

    public async Task<CommentResult> DeleteCommentAsync(Guid commentId, string requestingUserId)
    {
        var comment = await db.Comments
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment is null)
            return CommentResult.Failure("Comment not found.");

        if (!await CanAccessIssueAsync(comment.IssueId, requestingUserId))
            return CommentResult.Failure("You are not a member of this workspace.");

        // Author can always delete their own comment.
        // Owner and Admin can delete any comment — useful for moderation.
        if (comment.AuthorId != requestingUserId)
        {
            var role = await GetWorkspaceRoleForIssueAsync(comment.IssueId, requestingUserId);

            if (role is not (WorkspaceRole.Owner or WorkspaceRole.Admin))
                return CommentResult.Failure("You can only delete your own comments.");
        }

        var dto = MapToDto(comment);
        var issueId = comment.IssueId;
        var projectId = await GetProjectIdForIssueAsync(issueId);

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();

        await broadcaster.BroadcastCommentDeletedAsync(projectId, issueId, commentId, requestingUserId);

        return CommentResult.Success(dto);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Checks whether the user is a member of the workspace that owns the issue.
    /// </summary>
    private async Task<bool> CanAccessIssueAsync(Guid issueId, string userId)
    {
        var issue = await db.Issues
            .Include(i => i.Project)
            .FirstOrDefaultAsync(i => i.Id == issueId);

        if (issue is null) return false;

        return await db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == issue.Project.WorkspaceId && m.UserId == userId);
    }

    private async Task<Guid> GetProjectIdForIssueAsync(Guid issueId)
    {
        var issue = await db.Issues.FindAsync(issueId);
        return issue?.ProjectId ?? Guid.Empty;
    }

    private async Task<WorkspaceRole?> GetWorkspaceRoleForIssueAsync(Guid issueId, string userId)
    {
        var issue = await db.Issues
            .Include(i => i.Project)
            .FirstOrDefaultAsync(i => i.Id == issueId);

        if (issue is null) return null;

        return await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == issue.Project.WorkspaceId && m.UserId == userId)
            .Select(m => (WorkspaceRole?)m.Role)
            .FirstOrDefaultAsync();
    }

    private static CommentDto MapToDto(Comment comment) => new()
    {
        Id = comment.Id,
        IssueId = comment.IssueId,
        AuthorId = comment.AuthorId,
        AuthorDisplayName = comment.Author?.DisplayName ?? string.Empty,
        AuthorAvatarUrl = comment.Author?.AvatarUrl,
        Body = comment.Body,
        IsEdited = comment.IsEdited,
        CreatedAt = comment.CreatedAt,
        UpdatedAt = comment.UpdatedAt
    };
}