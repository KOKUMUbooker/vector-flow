using System.ComponentModel.DataAnnotations;

namespace VectorFlow.Shared.DTOs;

// ── Requests ──────────────────────────────────────────────────────────────────

public class CreateCommentRequest
{
    [Required]
    [MinLength(1)]
    public string Body { get; set; } = string.Empty;
}

public class UpdateCommentRequest
{
    [Required]
    [MinLength(1)]
    public string Body { get; set; } = string.Empty;
}

// ── Responses ─────────────────────────────────────────────────────────────────

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid IssueId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsEdited { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ── Service result ────────────────────────────────────────────────────────────

public class CommentResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public CommentDto? Comment { get; set; }

    public static CommentResult Success(CommentDto comment) =>
        new() { Succeeded = true, Comment = comment };

    public static CommentResult Failure(string error) =>
        new() { Error = error };
}