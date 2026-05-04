namespace VectorFlow.Api.Models;

public class Workspace
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier derived from the name. e.g. "My Team" → "my-team".
    /// Used in routes: /workspaces/{slug}/projects
    /// Must be unique across all workspaces.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// The user who created the workspace. This role cannot be transferred or removed.
    /// Stored separately from WorkspaceMember so it survives even if the member
    /// record is somehow corrupted.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public AppUser Owner { get; set; } = null!;
    public ICollection<WorkspaceMember> Members { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
    public ICollection<Invitation> Invitations { get; set; } = [];
}