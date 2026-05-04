using VectorFlow.Api.Enums;

namespace VectorFlow.Api.Models;

public class Invitation
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    /// <summary>
    /// The email address the invite was sent to.
    /// The recipient may not have a VectorFlow account yet at invite time.
    /// </summary>
    public string InvitedEmail { get; set; } = string.Empty;

    public string InvitedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Secure random token embedded in the invite link.
    /// Single-use — marked consumed once the recipient acts on it.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    /// <summary>Invitations expire after 7 days if not acted on.</summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Computed helper — not mapped
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    // Navigation properties
    public Workspace Workspace { get; set; } = null!;
    public AppUser InvitedBy { get; set; } = null!;
}