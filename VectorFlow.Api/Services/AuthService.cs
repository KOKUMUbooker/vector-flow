using VectorFlow.Api.Data;
using VectorFlow.Api.DTOs;
using VectorFlow.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Services;

public class AuthService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    IConfiguration configuration) : IAuthService
{
    public async Task<AuthResult?> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null) return null;

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid) return null;

        return await BuildAuthResultAsync(user);
    }


    public async Task<RegisterResult> RegisterAsync(RegisterRequest request)
    {
        var user = new AppUser
        {
            Email = request.Email,
            UserName = request.Email, // Identity uses UserName for lookups
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return RegisterResult.Failure(result.Errors.Select(e => e.Description));

        // TODO: send verification email here once email service is wired up

        return RegisterResult.Success();
    }


    public async Task<UserDto?> GetUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null ? null : MapToDto(user);
    }

    public async Task<AuthResult?> RefreshAsync(string refreshToken)
    {
        // Find the token record — eagerly load the user
        var stored = await db.RefreshTokens
            .Include(rt => rt.User)
            .SingleOrDefaultAsync(rt => rt.Token == refreshToken);

        // Reject if not found, already revoked, or expired
        if (stored is null || !stored.IsActive) return null;

        // Rotate — revoke the old token and issue a new one
        stored.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = await CreateRefreshTokenAsync(stored.User);
        await db.SaveChangesAsync();

        var accessToken = GenerateAccessToken(stored.User);

        return new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            User = MapToDto(stored.User)
        };
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var stored = await db.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.Token == refreshToken);

        if (stored is null || !stored.IsActive) return;

        stored.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    // =======  Private helpers ======================================

    /// <summary>
    /// Builds a full AuthResult for a user — generates access token,
    /// creates and persists a new refresh token, returns both.
    /// </summary>
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

    /// <summary>
    /// Generates a signed JWT access token for the given user.
    /// Short-lived — 15 minutes.
    /// </summary>
    private string GenerateAccessToken(AppUser user)
    {
        var secretKey = configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

        var keyBytes = Convert.FromBase64String(secretKey);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

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
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates a cryptographically secure refresh token and persists it.
    /// Does NOT call SaveChangesAsync — caller is responsible for saving.
    /// </summary>
    private async Task<RefreshToken> CreateRefreshTokenAsync(AppUser user)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(tokenBytes);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await db.RefreshTokens.AddAsync(refreshToken);
        return refreshToken;
    }

    private static UserDto MapToDto(AppUser user) => new()
    {
        Id = user.Id,
        DisplayName = user.DisplayName,
        Email = user.Email!,
        AvatarUrl = user.AvatarUrl
    };
}