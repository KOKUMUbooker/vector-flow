using System.ComponentModel.DataAnnotations;

namespace VectorFlow.Api.DTOs;

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
}

public class PasswordResetDto
{
    [Required]
    public required string PasswordVerificationToken { get; set; }

    [Required]
    [MinLength(8)]
    public required string NewPassword { get; set; }
}

public class ResendVerificationDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
}