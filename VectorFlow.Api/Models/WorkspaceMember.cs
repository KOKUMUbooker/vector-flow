using VectorFlow.Shared.Enums;

namespace VectorFlow.Api.Models;

/// <summary>
/// Junction table between Workspace and AppUser.
/// Represents a user's membership and role within a specific workspace.
/// </summary>
public class WorkspaceMember
{
    public Guid WorkspaceId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public WorkspaceRole Role { get; set; } = WorkspaceRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Workspace Workspace { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}