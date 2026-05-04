namespace VectorFlow.Api.Models;

public class RefreshToken
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The raw token value stored in the HttpOnly cookie.
    /// Store a SHA-256 hash of this in production for extra safety.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Null means the token is still valid (not yet revoked).
    /// Set this instead of deleting — gives you an audit trail of revocations.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Computed helpers — not mapped to DB columns
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsExpired && !IsRevoked;

    // Navigation property
    public AppUser User { get; set; } = null!;
}