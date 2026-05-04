namespace VectorFlow.Api.Models;

public class Label
{
    public Guid Id { get; set; }

    /// <summary>Labels are scoped to a project — not shared across projects.</summary>
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Hex color code for the label chip in the UI. e.g. "#E11D48".
    /// Stored with the hash prefix for direct use in CSS.
    /// </summary>
    public string Color { get; set; } = "#6B7280";

    // Navigation properties
    public Project Project { get; set; } = null!;
    public ICollection<IssueLabel> IssueLabels { get; set; } = [];
}

