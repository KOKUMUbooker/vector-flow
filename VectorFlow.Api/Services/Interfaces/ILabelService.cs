using VectorFlow.Api.DTOs;

namespace VectorFlow.Api.Services.Interfaces;

public interface ILabelService
{
    /// <summary>
    /// Returns all labels for a project.
    /// Any workspace member can view.
    /// </summary>
    Task<List<LabelDto>> GetLabelsAsync(Guid projectId, string requestingUserId);

    /// <summary>
    /// Creates a label scoped to a project.
    /// Owner/Admin only. Label name must be unique within the project.
    /// </summary>
    Task<LabelResult> CreateLabelAsync(Guid projectId, CreateLabelRequest request, string requestingUserId);

    /// <summary>
    /// Updates a label's name and color.
    /// Owner/Admin only. Changes reflect immediately on all issues using this label.
    /// </summary>
    Task<LabelResult> UpdateLabelAsync(Guid labelId, UpdateLabelRequest request, string requestingUserId);

    /// <summary>
    /// Deletes a label. Owner/Admin only.
    /// Removing a label detaches it from all issues silently via cascade on IssueLabel.
    /// </summary>
    Task<LabelResult> DeleteLabelAsync(Guid labelId, string requestingUserId);
}