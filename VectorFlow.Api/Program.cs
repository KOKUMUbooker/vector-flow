using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Resend;
using VectorFlow.Api.Data;
using VectorFlow.Api.Models;
using VectorFlow.Api.Services;
using VectorFlow.Api.Services.Interfaces;
using VectorFlow.Api.Hubs;
using VectorFlow.Api.Extensions;

namespace VectorFlow.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Load .env file into configuration
        DotNetEnv.Env.Load();
        builder.Configuration.AddEnvironmentVariables();

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings__DefaultConnection is not configured in .env.");
            options.UseNpgsql(connectionString);
        });

        builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;

            options.User.RequireUniqueEmail = true;

            // Email verification handled manually via own token flow
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();
        builder.Services.AddHttpClient<ResendClient>();
        builder.Services.AddSignalR();
        builder.Services.Configure<ResendClientOptions>( o =>
        {
            o.ApiToken = Environment.GetEnvironmentVariable("Resend__ApiKey")!;
        });


        // Configure a Development CORS policy
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DevCors", policy =>
            {
                policy.WithOrigins("http://localhost:5131")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // If using cookies/auth headers
            });
        }); 

        builder.Services.AddTransient<IResend, ResendClient>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        builder.Services.AddScoped<IInvitationService, InvitationService>();
        builder.Services.AddScoped<IIssueService, IssueService>();
        builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
        builder.Services.AddScoped<ICommentService, CommentService>();
        builder.Services.AddScoped<ILabelService, LabelService>();
        builder.Services.AddScoped<IProjectService, ProjectService>();
        builder.Services.AddScoped<IProjectHubBroadcaster, ProjectHubBroadcaster>();
        builder.Services.AddControllers();
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseCors("DevCors");
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseAuthentication();  
        app.UseAuthorization();

        app.MapHub<ProjectHub>("/hubs/project");
        app.MapControllers();

        app.Run();
    }
}