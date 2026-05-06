namespace VectorFlow.Api.DTOs;

using VectorFlow.Api.Enums;
using System.ComponentModel.DataAnnotations;

// ── Requests ─────────────────────────────────────────────────────────────────

public class CreateWorkspaceRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class UpdateWorkspaceRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class UpdateMemberRoleRequest
{
    [Required]
    public WorkspaceRole Role { get; set; }
}

// ── Responses ─────────────────────────────────────────────────────────────────

public class WorkspaceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>The calling user's role in this workspace.</summary>
    public WorkspaceRole CurrentUserRole { get; set; }
    public int MemberCount { get; set; }
    public int ProjectCount { get; set; }
}

public class WorkspaceMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public WorkspaceRole Role { get; set; }
    public DateTime JoinedAt { get; set; }

    /// <summary>True if this member is the workspace creator — role cannot be changed.</summary>
    public bool IsOwner { get; set; }
}

// ── Service result types ──────────────────────────────────────────────────────

public class WorkspaceResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public WorkspaceDto? Workspace { get; set; }

    public static WorkspaceResult Success(WorkspaceDto workspace) =>
        new() { Succeeded = true, Workspace = workspace };

    public static WorkspaceResult Failure(string error) =>
        new() { Error = error };
}

public class MemberResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }

    public static MemberResult Success() => new() { Succeeded = true };
    public static MemberResult Failure(string error) => new() { Error = error };
}