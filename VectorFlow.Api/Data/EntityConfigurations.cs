using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VectorFlow.Api.Models;

namespace VectorFlow.Api.Data.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.DisplayName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500);

        // RefreshTokens
        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Assigned issues — SetNull so deleting a user doesn't delete their issues
        builder.HasMany(u => u.AssignedIssues)
            .WithOne(i => i.Assignee)
            .HasForeignKey(i => i.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Comments
        builder.HasMany(u => u.Comments)
            .WithOne(c => c.Author)
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sent invitations
        builder.HasMany(u => u.SentInvitations)
            .WithOne(i => i.InvitedBy)
            .HasForeignKey(i => i.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .HasMaxLength(500)
            .IsRequired();

        // Ignore computed properties — EF should not try to map these to columns
        builder.Ignore(rt => rt.IsExpired);
        builder.Ignore(rt => rt.IsRevoked);
        builder.Ignore(rt => rt.IsActive);

        builder.HasIndex(rt => rt.Token).IsUnique();
        builder.HasIndex(rt => rt.UserId);
    }
}

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(w => w.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(w => w.Description)
            .HasMaxLength(500);

        // Owner — Restrict so you can't delete a user who owns a workspace
        builder.HasOne(w => w.Owner)
            .WithMany()
            .HasForeignKey(w => w.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Members
        builder.HasMany(w => w.Members)
            .WithOne(m => m.Workspace)
            .HasForeignKey(m => m.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Projects
        builder.HasMany(w => w.Projects)
            .WithOne(p => p.Workspace)
            .HasForeignKey(p => p.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Invitations
        builder.HasMany(w => w.Invitations)
            .WithOne(i => i.Workspace)
            .HasForeignKey(i => i.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => w.Slug).IsUnique();
    }
}

public class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        // Composite primary key — a user can only be a member of a workspace once
        builder.HasKey(m => new { m.WorkspaceId, m.UserId });

        builder.Property(m => m.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(m => m.User)
            .WithMany(u => u.WorkspaceMemberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvitedEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(i => i.Token)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Ignore(i => i.IsExpired);

        builder.HasIndex(i => i.Token).IsUnique();

        // Index to efficiently query pending invitations for a workspace
        builder.HasIndex(i => new { i.WorkspaceId, i.Status });
    }
}

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.KeyPrefix)
            .HasMaxLength(6)
            .IsRequired();

        // Labels
        builder.HasMany(p => p.Labels)
            .WithOne(l => l.Project)
            .HasForeignKey(l => l.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // KeyPrefix must be unique within a workspace
        builder.HasIndex(p => new { p.WorkspaceId, p.KeyPrefix }).IsUnique();
    }
}

public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Key)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Title)
            .HasMaxLength(500)
            .IsRequired();

        // Store enums as strings for readability in the DB and safe renaming
        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Reporter — Restrict, don't delete issues if reporter is removed
        builder.HasOne(i => i.Reporter)
            .WithMany()
            .HasForeignKey(i => i.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Comments
        builder.HasMany(i => i.Comments)
            .WithOne(c => c.Issue)
            .HasForeignKey(c => c.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        // Activity logs
        builder.HasMany(i => i.ActivityLogs)
            .WithOne(a => a.Issue)
            .HasForeignKey(a => a.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        // Key is unique within a project
        builder.HasIndex(i => new { i.ProjectId, i.Key }).IsUnique();
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.AssigneeId);
    }
}

public class IssueLabelConfiguration : IEntityTypeConfiguration<IssueLabel>
{
    public void Configure(EntityTypeBuilder<IssueLabel> builder)
    {
        builder.HasKey(il => new { il.IssueId, il.LabelId });

        builder.HasOne(il => il.Issue)
            .WithMany(i => i.IssueLabels)
            .HasForeignKey(il => il.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(il => il.Label)
            .WithMany(l => l.IssueLabels)
            .HasForeignKey(il => il.LabelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Body)
            .IsRequired();

        builder.HasIndex(c => c.IssueId);
    }
}

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.FromValue)
            .HasMaxLength(200);

        builder.Property(a => a.ToValue)
            .HasMaxLength(200);

        // Actor — Restrict so removing a user doesn't wipe the audit trail
        builder.HasOne(a => a.Actor)
            .WithMany()
            .HasForeignKey(a => a.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.IssueId);
    }
}

public class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.Color)
            .HasMaxLength(7) // "#RRGGBB"
            .IsRequired();

        // Label names must be unique within a project
        builder.HasIndex(l => new { l.ProjectId, l.Name }).IsUnique();
    }
}