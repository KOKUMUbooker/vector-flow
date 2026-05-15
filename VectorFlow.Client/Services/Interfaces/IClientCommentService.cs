using VectorFlow.Client.Services.Interfaces;
using VectorFlow.Shared.DTOs;

namespace VectorFlow.Client.Services.Interfaces;

public interface IClientCommentService
{
    /// <summary>
    /// Returns all comments for an issue ordered chronologically.
    /// </summary>
    Task<ServiceResult<List<CommentDto>>> GetCommentsAsync(Guid issueId);

    /// <summary>
    /// Adds a comment to an issue.
    /// </summary>
    Task<ServiceResult<CommentDto>> CreateCommentAsync(Guid issueId, CreateCommentRequest request);

    /// <summary>
    /// Edits a comment body.
    /// Marks the comment as edited and updates UpdatedAt.
    /// </summary>
    Task<ServiceResult<CommentDto>> UpdateCommentAsync(Guid commentId, UpdateCommentRequest request);

    /// <summary>
    /// Deletes a comment
    /// </summary>
    Task<ServiceResult> DeleteCommentAsync(Guid commentId);
}