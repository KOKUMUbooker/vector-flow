using VectorFlow.Api.DTOs;

namespace VectorFlow.Api.Services.Interfaces;

public interface ICommentService
{
    /// <summary>
    /// Returns all comments for an issue ordered chronologically.
    /// Any workspace member can view.
    /// </summary>
    Task<List<CommentDto>> GetCommentsAsync(Guid issueId, string requestingUserId);

    /// <summary>
    /// Adds a comment to an issue.
    /// Any workspace member can comment.
    /// </summary>
    Task<CommentResult> CreateCommentAsync(Guid issueId, CreateCommentRequest request, string authorId);

    /// <summary>
    /// Edits a comment body. Author only.
    /// Marks the comment as edited and updates UpdatedAt.
    /// </summary>
    Task<CommentResult> UpdateCommentAsync(Guid commentId, UpdateCommentRequest request, string requestingUserId);

    /// <summary>
    /// Deletes a comment. Author, Owner, or Admin only.
    /// </summary>
    Task<CommentResult> DeleteCommentAsync(Guid commentId, string requestingUserId);
}