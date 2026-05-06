namespace VectorFlow.Api.Services.Interfaces;

public interface IEmailTemplateService
{
    Task<string> GenerateVerificationEmail(string appName, string userName, string verificationUrl);
    Task<string> GeneratePasswordResetEmail(string appName, string userName, string resetUrl);
    Task<string> GenerateInvitationEmail(string appName, string recipientName, string inviterName, string workspaceName, string inviteUrl);
}
