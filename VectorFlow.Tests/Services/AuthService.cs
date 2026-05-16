// VectorFlow.Tests/Services/AuthServiceTests.cs
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Moq;
using VectorFlow.Api.Models;
using VectorFlow.Shared.DTOs;

namespace VectorFlow.Tests.Services;

public class AuthServiceTests : TestBase
{
    // ══════════════════════════════════════════════════════════════
    // LoginAsync
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithTokens()
    {
        // Arrange
        await CreateVerifiedUserAsync("user@example.com", "password");
        var service = BuildService();

        // Act
        var result = await service.LoginAsync(
            new LoginRequest { Email = "user@example.com", Password = "password" },
            baseUrl: "https://localhost");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.NotNull(result.User);
        Assert.Equal("user@example.com", result.User.Email);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsInvalidCredentials()
    {
        await CreateVerifiedUserAsync("user@example.com", "correctpassword");
        var service = BuildService();

        var result = await service.LoginAsync(
            new LoginRequest { Email = "user@example.com", Password = "wrongpassword" },
            baseUrl: "https://localhost");

        Assert.False(result.Succeeded);
        Assert.False(result.EmailNotVerified);
        Assert.Null(result.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmail_ReturnsInvalidCredentials()
    {
        var service = BuildService();

        var result = await service.LoginAsync(
            new LoginRequest { Email = "nobody@example.com", Password = "anything" },
            baseUrl: "https://localhost");

        Assert.False(result.Succeeded);
        Assert.False(result.EmailNotVerified);
    }

    [Fact]
    public async Task LoginAsync_UnverifiedEmail_ReturnsEmailNotVerified()
    {
        await CreateUnverifiedUserAsync("unverified@example.com", "pass");
        var service = BuildService();

        var result = await service.LoginAsync(
            new LoginRequest { Email = "unverified@example.com", Password = "pass" },
            baseUrl: "https://localhost");

        Assert.False(result.Succeeded);
        Assert.True(result.EmailNotVerified);
        Assert.NotNull(result.EmailVerificationToken);
    }

    [Fact]
    public async Task LoginAsync_UnverifiedEmail_SendsNewVerificationEmail()
    {
        await CreateUnverifiedUserAsync("unverified@example.com", "pass");
        var service = BuildService();

        await service.LoginAsync(
            new LoginRequest { Email = "unverified@example.com", Password = "pass" },
            baseUrl: "https://localhost");

        // Verify a verification email was sent exactly once
        EmailServiceMock.Verify(
            e => e.SendEmailAsync("unverified@example.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_Success_PersistsRefreshTokenToDb()
    {
        await CreateVerifiedUserAsync("user@example.com", "pass");
        var service = BuildService();

        await service.LoginAsync(
            new LoginRequest { Email = "user@example.com", Password = "pass" },
            baseUrl: "https://localhost");

        var tokenCount = await Db.RefreshTokens.CountAsync();
        Assert.Equal(1, tokenCount);
    }

    // ══════════════════════════════════════════════════════════════
    // RegisterAsync
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task RegisterAsync_NewUser_ReturnsSuccess()
    {
        var service = BuildService();

        var result = await service.RegisterAsync(
            new RegisterRequest
            {
                Email       = "new@example.com",
                Password    = "pass",
                DisplayName = "New User"
            },
            baseUrl: "https://localhost");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task RegisterAsync_NewUser_SendsVerificationEmail()
    {
        var service = BuildService();

        await service.RegisterAsync(
            new RegisterRequest
            {
                Email       = "new@example.com",
                Password    = "pass",
                DisplayName = "New User"
            },
            baseUrl: "https://localhost");

        EmailServiceMock.Verify(
            e => e.SendEmailAsync("new@example.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFailure()
    {
        await CreateVerifiedUserAsync("existing@example.com", "pass");
        var service = BuildService();

        var result = await service.RegisterAsync(
            new RegisterRequest
            {
                Email       = "existing@example.com",
                Password    = "pass",
                DisplayName = "Duplicate"
            },
            baseUrl: "https://localhost");

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task RegisterAsync_NewUser_CreatesUnverifiedAccount()
    {
        var service = BuildService();

        await service.RegisterAsync(
            new RegisterRequest
            {
                Email       = "new@example.com",
                Password    = "pass",
                DisplayName = "New User"
            },
            baseUrl: "https://localhost");

        // User should exist but not be email-confirmed
        var user = await Db.Users.FirstOrDefaultAsync(u => u.Email == "new@example.com");
        Assert.NotNull(user);
        Assert.False(user.EmailConfirmed);
        Assert.NotNull(user.EmailVerificationToken);
    }

    // ══════════════════════════════════════════════════════════════
    // VerifyEmailAsync
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task VerifyEmailAsync_ValidToken_ReturnsSuccess()
    {
        await CreateUnverifiedUserAsync();
        var service = BuildService();

        var result = await service.VerifyEmailAsync("valid-token");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.RedirectEmail);
    }

    [Fact]
    public async Task VerifyEmailAsync_ValidToken_MarksUserAsVerified()
    {
        var user = await CreateUnverifiedUserAsync();
        var service = BuildService();

        await service.VerifyEmailAsync("valid-token");

        // Reload from DB to confirm the change was persisted
        await Db.Entry(user).ReloadAsync();
        Assert.True(user.EmailConfirmed);
        Assert.Null(user.EmailVerificationToken);
    }

    [Fact]
    public async Task VerifyEmailAsync_InvalidToken_ReturnsInvalid()
    {
        var service = BuildService();

        var result = await service.VerifyEmailAsync("bogus-token");

        Assert.False(result.Succeeded);
        Assert.False(result.AlreadyVerified);
        Assert.False(result.TokenExpired);
    }

    [Fact]
    public async Task VerifyEmailAsync_AlreadyVerified_ReturnsAlreadyVerified()
    {
        // Create an already-verified user and give them a token anyway
        var user = await CreateVerifiedUserAsync();
        user.EmailVerificationToken = "some-token";
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await Db.SaveChangesAsync();

        var service = BuildService();
        var result  = await service.VerifyEmailAsync("some-token");

        Assert.True(result.AlreadyVerified);
    }

    [Fact]
    public async Task VerifyEmailAsync_ExpiredToken_ReturnsExpired()
    {
        var user = await CreateUnverifiedUserAsync();

        // Backdate the expiry
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(-1);
        await Db.SaveChangesAsync();

        var service = BuildService();
        var result  = await service.VerifyEmailAsync("valid-token");

        Assert.True(result.TokenExpired);
    }

    // ══════════════════════════════════════════════════════════════
    // ResetPasswordAsync
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_ReturnsSuccess()
    {
        var user    = await CreateVerifiedUserAsync("user@example.com", "oldpass");
        var service = BuildService();

        // Simulate what ForgotPassword does — store a hashed token
        var rawToken    = "my-raw-reset-token";
        var hashedToken = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        user.PasswordResetTokenHash   = hashedToken;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);
        await Db.SaveChangesAsync();

        var result = await service.ResetPasswordAsync(new PasswordResetDto
        {
            PasswordVerificationToken = rawToken,
            NewPassword               = "newpass123"
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_RevokesAllRefreshTokens()
    {
        var user    = await CreateVerifiedUserAsync("user@example.com", "oldpass");
        var service = BuildService();

        // Plant an active refresh token for this user
        await Db.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId    = user.Id,
            Token     = "active-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        });

        var rawToken    = "reset-token";
        var hashedToken = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        user.PasswordResetTokenHash   = hashedToken;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);
        await Db.SaveChangesAsync();

        await service.ResetPasswordAsync(new PasswordResetDto
        {
            PasswordVerificationToken = rawToken,
            NewPassword               = "newpass123"
        });

        // All refresh tokens should now be revoked
        var activeTokens = await Db.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
            .CountAsync();

        Assert.Equal(0, activeTokens);
    }

    [Fact]
    public async Task ResetPasswordAsync_InvalidToken_ReturnsFailure()
    {
        var service = BuildService();

        var result = await service.ResetPasswordAsync(new PasswordResetDto
        {
            PasswordVerificationToken = "completely-wrong-token",
            NewPassword               = "newpass"
        });

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredToken_ReturnsFailure()
    {
        var user    = await CreateVerifiedUserAsync();
        var service = BuildService();

        var rawToken    = "expired-token";
        var hashedToken = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        user.PasswordResetTokenHash   = hashedToken;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(-1); // already expired
        await Db.SaveChangesAsync();

        var result = await service.ResetPasswordAsync(new PasswordResetDto
        {
            PasswordVerificationToken = rawToken,
            NewPassword               = "newpass"
        });

        Assert.False(result.Succeeded);
    }

    // ══════════════════════════════════════════════════════════════
    // RefreshAsync
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task RefreshAsync_ValidActiveToken_ReturnsNewTokenPair()
    {
        var user    = await CreateVerifiedUserAsync();
        var service = BuildService();

        // Plant an active refresh token directly in the DB
        var oldToken = new RefreshToken
        {
            UserId    = user.Id,
            Token     = "valid-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await Db.RefreshTokens.AddAsync(oldToken);
        await Db.SaveChangesAsync();

        var result = await service.RefreshAsync("valid-refresh-token");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        // New token must be different from the old one (rotation)
        Assert.NotEqual("valid-refresh-token", result.RefreshToken);
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_RevokesOldToken()
    {
        var user = await CreateVerifiedUserAsync();
        var service = BuildService();

        var oldToken = new RefreshToken
        {
            UserId    = user.Id,
            Token     = "old-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await Db.RefreshTokens.AddAsync(oldToken);
        await Db.SaveChangesAsync();

        await service.RefreshAsync("old-token");

        var stored = await Db.RefreshTokens
            .FirstAsync(rt => rt.Token == "old-token");

        Assert.NotNull(stored.RevokedAt);
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ReturnsFailure()
    {
        var user = await CreateVerifiedUserAsync();
        var service = BuildService();

        var expiredToken = new RefreshToken
        {
            UserId    = user.Id,
            Token     = "expired-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // already expired
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };
        await Db.RefreshTokens.AddAsync(expiredToken);
        await Db.SaveChangesAsync();

        var result = await service.RefreshAsync("expired-token");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task RefreshAsync_RevokedToken_ReturnsFailure()
    {
        var user = await CreateVerifiedUserAsync();
        var service = BuildService();

        var revokedToken = new RefreshToken
        {
            UserId    = user.Id,
            Token     = "revoked-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow.AddHours(-1) // already revoked
        };
        await Db.RefreshTokens.AddAsync(revokedToken);
        await Db.SaveChangesAsync();

        var result = await service.RefreshAsync("revoked-token");

        Assert.False(result.Succeeded);
    }

    // ══════════════════════════════════════════════════════════════
    // RevokeRefreshTokenAsync
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task RevokeRefreshTokenAsync_ActiveToken_SetsRevokedAt()
    {
        var user = await CreateVerifiedUserAsync();
        var service = BuildService();

        var token = new RefreshToken
        {
            UserId    = user.Id,
            Token     = "to-revoke",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await Db.RefreshTokens.AddAsync(token);
        await Db.SaveChangesAsync();

        await service.RevokeRefreshTokenAsync("to-revoke");

        var stored = await Db.RefreshTokens.FirstAsync(rt => rt.Token == "to-revoke");
        Assert.NotNull(stored.RevokedAt);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_NonExistentToken_DoesNotThrow()
    {
        var service = BuildService();

        // Should silently do nothing — no exception
        var ex = await Record.ExceptionAsync(
            () => service.RevokeRefreshTokenAsync("does-not-exist"));

        Assert.Null(ex);
    }

    // ══════════════════════════════════════════════════════════════
    // GetUserAsync
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetUserAsync_ExistingUser_ReturnsUserDto()
    {
        var user    = await CreateVerifiedUserAsync("user@example.com", "pass", "Test User");
        var service = BuildService();

        var dto = await service.GetUserAsync(user.Id);

        Assert.NotNull(dto);
        Assert.Equal(user.Id,    dto.Id);
        Assert.Equal("Test User", dto.DisplayName);
        Assert.Equal("user@example.com", dto.Email);
    }

    [Fact]
    public async Task GetUserAsync_NonExistentId_ReturnsNull()
    {
        var service = BuildService();

        var dto = await service.GetUserAsync(Guid.NewGuid().ToString());

        Assert.Null(dto);
    }
}