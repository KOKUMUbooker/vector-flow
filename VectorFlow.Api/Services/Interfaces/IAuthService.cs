using VectorFlow.Api.DTOs;

namespace VectorFlow.Api.Services.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Validates credentials and returns tokens + user details on success.
    /// Returns null if the email doesn't exist or password is wrong.
    /// </summary>
    Task<AuthResult?> LoginAsync(LoginRequest request);

    /// <summary>
    /// Creates a new user account and sends a verification email.
    /// Returns a result indicating success or validation errors.
    /// </summary>
    Task<RegisterResult> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Returns safe user details for the currently authenticated user.
    /// Used by GET /auth/me to restore Blazor auth state on page reload.
    /// Returns null if the user no longer exists.
    /// </summary>
    Task<UserDto?> GetUserAsync(string userId);

    /// <summary>
    /// Validates the incoming refresh token, rotates it (issues a new one),
    /// and returns a fresh access token + refresh token pair.
    /// Returns null if the token is invalid, expired, or revoked.
    /// </summary>
    Task<AuthResult?> RefreshAsync(string refreshToken);

    /// <summary>
    /// Marks the given refresh token as revoked in the database.
    /// Called on logout to invalidate the session server-side.
    /// </summary>
    Task RevokeRefreshTokenAsync(string refreshToken);
}