namespace VectorFlow.Api.Models;

public class Comment
{
    public Guid Id { get; set; }

    public Guid IssueId { get; set; }

    public string AuthorId { get; set; } = string.Empty;

    /// <summary>Markdown-formatted comment body.</summary>
    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Updated whenever the author edits the comment.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// True if the comment has been edited at least once after creation.
    /// Shown as an "edited" indicator in the UI.
    /// </summary>
    public bool IsEdited { get; set; } = false;

    // Navigation properties
    public Issue Issue { get; set; } = null!;
    public AppUser Author { get; set; } = null!;
}