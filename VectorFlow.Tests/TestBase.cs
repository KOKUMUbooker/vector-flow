using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VectorFlow.Api.Data;
using VectorFlow.Api.Models;
using VectorFlow.Api.Services.Interfaces;
using VectorFlow.Api.Services;

namespace VectorFlow.Tests;

public abstract class TestBase : IDisposable
{
    protected readonly AppDbContext Db;
    protected readonly UserManager<AppUser> UserManager;
    protected readonly IConfiguration Configuration;
    protected readonly Mock<IEmailService> EmailServiceMock;
    protected readonly Mock<IEmailTemplateService> TemplateServiceMock;

    protected TestBase()
    {
        // ── In-memory database ─────────────────────────────────────────────
        // Each test gets a uniquely named DB so tests don't bleed into each other.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Db = new AppDbContext(options);

        // ── UserManager ────────────────────────────────────────────────────
        // UserManager has a long constructor — easier to build via DI than manually.
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddIdentity<AppUser, IdentityRole>(opts =>
        {
            // Relax password rules for tests — makes creating users less verbose
            opts.Password.RequireDigit           = false;
            opts.Password.RequireLowercase        = false;
            opts.Password.RequireUppercase        = false;
            opts.Password.RequireNonAlphanumeric  = false;
            opts.Password.RequiredLength          = 4;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();

        // Use the same Db instance across both UserManager and our service
        UserManager = provider.GetRequiredService<UserManager<AppUser>>();

        // ── Configuration ─────────────────────────────────────────────────
        // Fake JWT settings — enough for the service to generate tokens.
        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"]                    = Convert.ToBase64String(new byte[32]),
                ["JwtSettings:Issuer"]                       = "test-issuer",
                ["JwtSettings:Audience"]                     = "test-audience",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "15",
                ["JwtSettings:RefreshTokenExpirationDays"]   = "7"
            })
            .Build();

        // ── Mocks ──────────────────────────────────────────────────────────
        // Email and template services have external side effects — mock them.
        // Most tests just verify they were called; we don't care about the HTML body.
        EmailServiceMock    = new Mock<IEmailService>();
        TemplateServiceMock = new Mock<IEmailTemplateService>();

        // Default: template service returns a non-null string so the service
        // doesn't throw when it tries to send the email.
        TemplateServiceMock
            .Setup(t => t.GenerateVerificationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("<html>verify</html>");

        TemplateServiceMock
            .Setup(t => t.GeneratePasswordResetEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("<html>reset</html>");

        EmailServiceMock
            .Setup(e => e.SendEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Helper — creates and persists a verified user so tests that need
    /// an existing account don't repeat this boilerplate every time.
    /// </summary>
    protected async Task<AppUser> CreateVerifiedUserAsync(
        string email    = "test@example.com",
        string password = "pass",
        string name     = "Test User")
    {
        var user = new AppUser
        {
            Email          = email,
            UserName       = email,
            DisplayName    = name,
            EmailConfirmed = true,
            CreatedAt      = DateTime.UtcNow
        };

        var result = await UserManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new Exception($"Test setup failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return user;
    }

    /// <summary>Helper — creates an unverified user with a pending verification token.</summary>
    protected async Task<AppUser> CreateUnverifiedUserAsync(
        string email = "unverified@example.com",
        string password = "pass")
    {
        var user = new AppUser
        {
            Email                        = email,
            UserName                     = email,
            DisplayName                  = "Unverified User",
            EmailConfirmed               = false,
            EmailVerificationToken       = "valid-token",
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            CreatedAt                    = DateTime.UtcNow
        };

        await UserManager.CreateAsync(user, password);
        return user;
    }

    /// <summary>Builds the service under test using the shared test infrastructure.</summary>
    protected AuthService BuildService() => new(
        UserManager,
        Db,
        Configuration,
        EmailServiceMock.Object,
        TemplateServiceMock.Object);

    public void Dispose() => Db.Dispose();
}