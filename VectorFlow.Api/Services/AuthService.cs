using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VectorFlow.Api.Data;
using VectorFlow.Shared.DTOs;
using VectorFlow.Api.Models;
using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Services;

public class AuthService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    IConfiguration configuration,
    IEmailService emailService,
    IEmailTemplateService templateService) : IAuthService
{
    private const string AppName = "VectorFlow";

    // ── Login ────────────────────────────────────────────────────────────────

public async Task<AuthResult> LoginAsync(LoginRequest request)
{
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user is null) return AuthResult.InvalidCredentials();

    var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
    if (!passwordValid) return AuthResult.InvalidCredentials();

    // Check email verification before issuing tokens
    if (!user.EmailConfirmed)
    {
        // Rotate the verification token on each failed login attempt
        // so the link in any previous email is immediately invalidated
        user.EmailVerificationToken = GenerateSecureToken();
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await db.SaveChangesAsync();

        // Send a fresh verification email automatically
        // so the user doesn't have to go request one manually
        var baseUrl = configuration["ApiBaseUrl"]
            ?? throw new InvalidOperationException("ApiBaseUrl not configured.");
        var verificationUrl = $"{baseUrl}/api/auth/verify-email?token={user.EmailVerificationToken}";
        var html = await templateService.GenerateVerificationEmail(AppName, user.DisplayName, verificationUrl);
        await emailService.SendEmailAsync(user.Email!, "Verify your email", html);

        return AuthResult.UnverifiedEmail(user.EmailVerificationToken);
    }

    return await BuildAuthResultAsync(user);
}

    // ── Register ──────────────────────────────────────────────────────────────

    public async Task<RegisterResult> RegisterAsync(RegisterRequest request, string baseUrl)
    {
        var user = new AppUser
        {
            Email = request.Email,
            UserName = request.Email,
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow,
            EmailVerificationToken = GenerateSecureToken(),
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return RegisterResult.Failure(result.Errors.Select(e => e.Description));

        var verificationUrl = $"{baseUrl}/api/auth/verify-email?token={user.EmailVerificationToken}";
        var html = await templateService.GenerateVerificationEmail(AppName, user.DisplayName, verificationUrl);
        await emailService.SendEmailAsync(user.Email!, "Verify your email", html);

        return RegisterResult.Success();
    }

    // ── Get current user ──────────────────────────────────────────────────────
    public async Task<UserDto?> GetUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null ? null : MapToDto(user);
    }

    // ── Email verification ────────────────────────────────────────────────────
    public async Task<EmailVerificationResult> VerifyEmailAsync(string token)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

        if (user is null) return EmailVerificationResult.Invalid();
        if (user.EmailConfirmed) return EmailVerificationResult.Already();
        if (user.EmailVerificationTokenExpiry < DateTime.UtcNow) return EmailVerificationResult.Expired();

        user.EmailConfirmed = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        await db.SaveChangesAsync();

        return EmailVerificationResult.Success(user.Email!);
    }

    // ── Resend verification ───────────────────────────────────────────────────
    public async Task ResendVerificationAsync(string email, string baseUrl)
    {
        var user = await userManager.FindByEmailAsync(email);

        // Silently exit — don't reveal whether the email is registered
        if (user is null || user.EmailConfirmed) return;

        user.EmailVerificationToken = GenerateSecureToken();
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await db.SaveChangesAsync();

        var verificationUrl = $"{baseUrl}/api/auth/verify-email?token={user.EmailVerificationToken}";
        var html = await templateService.GenerateVerificationEmail(AppName, user.DisplayName, verificationUrl);
        await emailService.SendEmailAsync(user.Email!, "Verify your email", html);
    }

    // ── Forgot password ───────────────────────────────────────────────────────
    public async Task ForgotPasswordAsync(string email, string baseUrl)
    {
        var user = await userManager.FindByEmailAsync(email);

        // Always return silently — never reveal whether the email exists
        if (user is null) return;

        var rawToken = GenerateSecureToken();
        var hashedToken = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        user.PasswordResetTokenHash = hashedToken;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);
        await db.SaveChangesAsync();

        var resetUrl = $"{baseUrl}/reset-password?tkn={rawToken}&email={user.Email}";
        var html = await templateService.GeneratePasswordResetEmail(AppName, user.DisplayName, resetUrl);
        await emailService.SendEmailAsync(user.Email!, "Reset your password", html);
    }

    // ── Reset password ────────────────────────────────────────────────────────
    public async Task<PasswordResetResult> ResetPasswordAsync(PasswordResetDto dto)
    {
        var hashedToken = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(dto.PasswordVerificationToken)));

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.PasswordResetTokenHash == hashedToken);

        if (user is null)
            return PasswordResetResult.Failure("Invalid or expired token.");

        if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return PasswordResetResult.Failure("Invalid or expired token.");

        // Delegate hashing to Identity — keeps algorithm consistent
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);

        if (!result.Succeeded)
            return PasswordResetResult.Failure(
                string.Join(", ", result.Errors.Select(e => e.Description)));

        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiry = null;

        // Revoke all active sessions — user changed their password
        await db.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAt, DateTime.UtcNow));

        await db.SaveChangesAsync();

        return PasswordResetResult.Success();
    }

    // ── Refresh ───────────────────────────────────────────────────────────────
    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        var stored = await db.RefreshTokens
            .Include(rt => rt.User)
            .SingleOrDefaultAsync(rt => rt.Token == refreshToken);

        if (stored is null || !stored.IsActive) return AuthResult.InvalidCredentials();

        stored.RevokedAt = DateTime.UtcNow;
        var newToken = await CreateRefreshTokenAsync(stored.User);
        await db.SaveChangesAsync();

        return new AuthResult
        {
            AccessToken = GenerateAccessToken(stored.User),
            RefreshToken = newToken.Token,
            User = MapToDto(stored.User)
        };
    }

    // ── Revoke ────────────────────────────────────────────────────────────────
    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var stored = await db.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.Token == refreshToken);

        if (stored is null || !stored.IsActive) return;

        stored.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    // ── Private helpers ───────────────────────────────────────────────────────
    private async Task<AuthResult> BuildAuthResultAsync(AppUser user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user);
        await db.SaveChangesAsync();

        return new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            User = MapToDto(user)
        };
    }

    private string GenerateAccessToken(AppUser user)
    {
        var secretKey = configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey not configured.");
        var accessTokenExpirationMinutes = 
            int.TryParse(configuration["JwtSettings:AccessTokenExpirationMinutes"], out var val) ? val : 15;

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Convert.FromBase64String(secretKey)),
            SecurityAlgorithms.HmacSha256
        );

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("displayName", user.DisplayName)
        };

        var token = new JwtSecurityToken(
            issuer: configuration["JwtSettings:Issuer"],
            audience: configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(accessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(AppUser user)
    {
        var refreshTokenExpirationDays = 
            int.TryParse(configuration["JwtSettings:RefreshTokenExpirationDays"], out var val) ? val : 7;

        var token = new RefreshToken
        {
            UserId = user.Id,
            Token = GenerateSecureToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        await db.RefreshTokens.AddAsync(token);
        return token;
    }

    /// <summary>
    /// URL-safe base64 token — safe to embed in query strings without encoding.
    /// </summary>
    private static string GenerateSecureToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

    private static UserDto MapToDto(AppUser user) => new()
    {
        Id = user.Id,
        DisplayName = user.DisplayName,
        Email = user.Email!,
        AvatarUrl = user.AvatarUrl
    };
}