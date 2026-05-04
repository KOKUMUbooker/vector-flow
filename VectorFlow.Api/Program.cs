using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VectorFlow.Api.Data;
using VectorFlow.Api.Models;

namespace VectorFlow.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Load .env file into configuration
        DotNetEnv.Env.Load();

        // ── Database ────────────────────────────────────────────────
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // ── Identity ─────────────────────────────────────────────────
        builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;

            options.User.RequireUniqueEmail = true;

            // We handle email verification manually via our own token flow
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // ── Other services ───────────────────────────────────────────
        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();  
        app.UseAuthorization();

        app.Run();
    }
}