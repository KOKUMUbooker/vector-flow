using Microsoft.EntityFrameworkCore;
using VectorFlow.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace VectorFlow.Api.Data;

public class AppDbContext (DbContextOptions<AppDbContext> options) 
 : IdentityDbContext<AppUser>(options)
 {    
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<IssueLabel> IssueLabels => Set<IssueLabel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Must be called first — wires up all ASP.NET Identity tables
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("app");

        // Picks up every IEntityTypeConfiguration<T> in this assembly automatically.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}