using VectorFlow.Shared.DTOs;

namespace VectorFlow.Api.Services.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Validates credentials and returns tokens + user details on success.
    /// Returns null if the email doesn't exist or password is wrong.
    /// </summary>
    Task<AuthResult> LoginAsync(LoginRequest request,string baseUrl);

    /// <summary>
    /// Creates a new user account and sends a verification email.
    /// Returns a result indicating success or validation errors.
    /// </summary>
    Task<RegisterResult> RegisterAsync(RegisterRequest request, string baseUrl);

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
    Task<AuthResult> RefreshAsync(string refreshToken);

    /// <summary>
    /// Marks the given refresh token as revoked in the database.
    /// Called on logout to invalidate the session server-side.
    /// </summary>
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task<EmailVerificationResult> VerifyEmailAsync(string token);
    Task ResendVerificationAsync(string email, string baseUrl);
    Task ForgotPasswordAsync(string email, string baseUrl);
    Task<PasswordResetResult> ResetPasswordAsync(PasswordResetDto dto);
}

public class EmailVerificationResult
{
    public bool Succeeded { get; set; }
    public bool AlreadyVerified { get; set; }
    public bool TokenExpired { get; set; }
    public string? RedirectEmail { get; set; }
 
    public static EmailVerificationResult Success(string email) =>
        new() { Succeeded = true, RedirectEmail = email };
    public static EmailVerificationResult Already() =>
        new() { AlreadyVerified = true };
    public static EmailVerificationResult Expired() =>
        new() { TokenExpired = true };
    public static EmailVerificationResult Invalid() => new();
}
 
public class PasswordResetResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
 
    public static PasswordResetResult Success() => new() { Succeeded = true };
    public static PasswordResetResult Failure(string error) => new() { Error = error };
}