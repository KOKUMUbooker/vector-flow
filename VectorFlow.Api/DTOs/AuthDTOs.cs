using System.ComponentModel.DataAnnotations;

namespace VectorFlow.Api.DTOs;

// ── Requests ────────────────────────────────────────────────────────────────

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

// ── Responses ───────────────────────────────────────────────────────────────

/// <summary>
/// Returned to the client after a successful login or token refresh.
/// The tokens themselves are attached as HttpOnly cookies — not in this body.
/// This body only carries the user details the client needs to hydrate auth state.
/// </summary>
public class AuthResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>User details returned to the client for UI state.</summary>
    public UserDto User { get; set; } = null!;
}

/// <summary>
/// Returned by RegisterAsync. Mirrors IdentityResult's shape so the
/// controller can forward validation errors directly to the client.
/// </summary>
public class RegisterResult
{
    public bool Succeeded { get; set; }
    public IEnumerable<string> Errors { get; set; } = [];

    public static RegisterResult Success() =>
        new() { Succeeded = true };

    public static RegisterResult Failure(IEnumerable<string> errors) =>
        new() { Succeeded = false, Errors = errors };
}

/// <summary>
/// Safe user representation sent to the client.
/// Never expose PasswordHash, SecurityStamp, or other sensitive Identity fields.
/// </summary>
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}