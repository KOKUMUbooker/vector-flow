using System.ComponentModel.DataAnnotations;

namespace VectorFlow.Api.DTOs;

// ── Requests ──────────────────────────────────────────────────────────────────

public class CreateLabelRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Hex color code including the # prefix. e.g. "#E11D48".</summary>
    [Required]
    [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "Color must be a valid hex code e.g. #E11D48")]
    public string Color { get; set; } = "#6B7280";
}

public class UpdateLabelRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "Color must be a valid hex code e.g. #E11D48")]
    public string Color { get; set; } = string.Empty;
}

// ── Service result ────────────────────────────────────────────────────────────

public class LabelResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public LabelDto? Label { get; set; }

    public static LabelResult Success(LabelDto label) =>
        new() { Succeeded = true, Label = label };

    public static LabelResult Failure(string error) =>
        new() { Error = error };
}