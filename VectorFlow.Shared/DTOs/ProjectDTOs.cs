using System.ComponentModel.DataAnnotations;

namespace VectorFlow.Shared.DTOs;

// ── Requests ──────────────────────────────────────────────────────────────────

public class CreateProjectRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 2–6 uppercase characters used to prefix issue keys. e.g. "VF" → VF-1, VF-2.
    /// Must be unique within the workspace.
    /// </summary>
    [Required]
    [MinLength(2)]
    [MaxLength(6)]
    [RegularExpression("^[A-Z0-9]+$", ErrorMessage = "Key prefix must be uppercase letters and numbers only.")]
    public string KeyPrefix { get; set; } = string.Empty;
}

public class UpdateProjectRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

// ── Responses ─────────────────────────────────────────────────────────────────

public class ProjectDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string KeyPrefix { get; set; } = string.Empty;
    public int IssueCounter { get; set; }
    public DateTime CreatedAt { get; set; }
    public int IssueCount { get; set; }
    public int LabelCount { get; set; }
}

// ── Service result ────────────────────────────────────────────────────────────

public class ProjectResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public ProjectDto? Project { get; set; }

    public static ProjectResult Success(ProjectDto project) =>
        new() { Succeeded = true, Project = project };

    public static ProjectResult Failure(string error) =>
        new() { Error = error };
}