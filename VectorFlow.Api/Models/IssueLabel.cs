namespace VectorFlow.Api.Models;

/// <summary>
/// Explicit junction table for the Issue ↔ Label many-to-many relationship.
/// Using an explicit entity (rather than EF's implicit join) gives us
/// the flexibility to add metadata later (e.g. added by, added at).
/// </summary>
public class IssueLabel
{
    public Guid IssueId { get; set; }
    public Guid LabelId { get; set; }

    // Navigation properties
    public Issue Issue { get; set; } = null!;
    public Label Label { get; set; } = null!;
}