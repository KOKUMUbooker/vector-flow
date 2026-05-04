using Microsoft.AspNetCore.Identity;

namespace VectorFlow.Api.Models;

/// <summary>
/// Extends ASP.NET Identity's IdentityUser with VectorFlow-specific fields.
/// IdentityUser already provides: Id, Email, UserName, PasswordHash,
/// EmailConfirmed, PhoneNumber, SecurityStamp, ConcurrencyStamp, etc.
/// </summary>
public class AppUser : IdentityUser
{
    /// <summary>The name shown across the UI — not the login email.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Optional profile picture URL. Null means use a generated avatar.</summary>
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<WorkspaceMember> WorkspaceMemberships { get; set; } = [];
    public ICollection<Issue> AssignedIssues { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<Invitation> SentInvitations { get; set; } = [];
}