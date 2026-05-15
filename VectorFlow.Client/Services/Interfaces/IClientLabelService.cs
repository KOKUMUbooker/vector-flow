using VectorFlow.Shared.DTOs;

namespace VectorFlow.Client.Services.Interfaces;

public interface IClientLabelService
{
    /// <summary>
    /// Returns all labels for a project.
    /// </summary>
    Task<ServiceResult<List<LabelDto>>> GetLabelsAsync(Guid projectId);

    /// <summary>
    /// Creates a label scoped to a project.
    /// </summary>
    Task<ServiceResult<LabelDto>> CreateLabelAsync(Guid projectId, CreateLabelRequest request);

    /// <summary>
    /// Updates a label's name and color.
    /// </summary>
    Task<ServiceResult<LabelDto>> UpdateLabelAsync(Guid labelId, UpdateLabelRequest request);

    /// <summary>
    /// Deletes a label. Owner/Admin only.
    /// </summary>
    Task<ServiceResult> DeleteLabelAsync(Guid labelId);
}