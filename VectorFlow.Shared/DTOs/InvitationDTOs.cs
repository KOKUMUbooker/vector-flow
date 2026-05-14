using System.ComponentModel.DataAnnotations;
using VectorFlow.Shared.Enums;

namespace VectorFlow.Shared.DTOs;

// ── Requests ──────────────────────────────────────────────────────────────────

public class SendInvitationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

// ── Responses ─────────────────────────────────────────────────────────────────

public class InvitationDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string WorkspaceName { get; set; } = string.Empty;
    public string WorkspaceSlug { get; set; } = string.Empty;
    public string InvitedEmail { get; set; } = string.Empty;
    public string InvitedByDisplayName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public InvitationStatus Status { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Service result ────────────────────────────────────────────────────────────

public class InvitationResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public string WorkspaceSlug { get; set; } = string.Empty;
    public InvitationDto? Invitation { get; set; }

    public static InvitationResult Success(InvitationDto invitation, string workspaceSlug) =>
        new() { Succeeded = true, Invitation = invitation, WorkspaceSlug = workspaceSlug };

    public static InvitationResult Failure(string error) =>
        new() { Error = error };
}