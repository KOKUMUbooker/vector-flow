namespace VectorFlow.Api.Services.Interfaces;

public interface IEmailTemplateService
{
    Task<string> GenerateVerificationEmail(string appName, string userName, string verificationUrl);
    Task<string> GeneratePasswordResetEmail(string appName, string userName, string resetUrl);
}
