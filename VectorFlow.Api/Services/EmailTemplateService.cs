using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IWebHostEnvironment _environment;
    private readonly Dictionary<string, string> _templateCache = new();

    public EmailTemplateService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    private async Task<string> LoadTemplateAsync(string templateName)
    {
        if (_templateCache.TryGetValue(templateName, out var cached))
            return cached;

        var filePath = Path.Combine(_environment.ContentRootPath, "EmailTemplates", templateName);

        string template;

        if (File.Exists(filePath)) template = await File.ReadAllTextAsync(filePath);
        else
        {
            template = templateName switch
            {
                "EmailVerificationTemplate.html" => GetFallbackVerificationTemplate(),
                "PasswordResetTemplate.html" => GetFallbackPasswordResetTemplate(),
                "WorkspaceInvitationEmail.html" => GetFallbackWorkSpaceInvitationTemplate(),
                _ => throw new FileNotFoundException($"Template not found: {templateName}")
            };
        }

        _templateCache[templateName] = template;
        return template;
    }


    public async Task<string> GenerateVerificationEmail(string appName, string userName, string verificationUrl)
    {
        var template = await LoadTemplateAsync("EmailVerificationTemplate.html");

        return template
            .Replace("{{AppName}}", appName)
            .Replace("{{UserName}}", userName)
            .Replace("{{VerificationUrl}}", verificationUrl)
            .Replace("{{CurrentYear}}", DateTime.Now.Year.ToString());
    }

    public async Task<string> GeneratePasswordResetEmail(string appName, string userName, string resetUrl)
    {
        var template = await LoadTemplateAsync("PasswordResetTemplate.html");

        return template
            .Replace("{{AppName}}", appName)
            .Replace("{{UserName}}", userName)
            .Replace("{{ResetUrl}}", resetUrl)
            .Replace("{{CurrentYear}}", DateTime.Now.Year.ToString());
    }

    public async Task<string> GenerateInvitationEmail(string appName, string recipientName, string inviterName, string workspaceName, string inviteUrl) {
        var template = await LoadTemplateAsync("WorkspaceInvitationEmail.html");

        return template
           .Replace("{{AppName}}", appName)
           .Replace("{{recipientName}}", recipientName)
           .Replace("{{inviterName}}", inviterName)
           .Replace("{{workspaceName}}", workspaceName)
           .Replace("{{inviteUrl}}", inviteUrl)
           .Replace("{{CurrentYear}}", DateTime.Now.Year.ToString());
    }


    // Fallback templates in case HTML files are not found
    private static string GetFallbackVerificationTemplate()
    {
        return @"<!DOCTYPE html><html><body>
            <h2>Verify Your Email</h2>
            <p>Hi {{UserName}},</p>
            <p>Welcome to {{AppName}}! Please verify your email by clicking: <a href='{{VerificationUrl}}'>{{VerificationUrl}}</a></p>
            <p>This link expires in 24 hours.</p>
            <p>&copy; {{CurrentYear}} {{AppName}}</p>
        </body></html>";
    }

    private static string GetFallbackPasswordResetTemplate()
    {
        return @"<!DOCTYPE html><html><body>
            <h2>Reset Your Password</h2>
            <p>Hi {{UserName}},</p>
            <p>Reset your {{AppName}} password by clicking: <a href='{{ResetUrl}}'>{{ResetUrl}}</a></p>
            <p>This link expires in 1 hour.</p>
            <p>&copy; {{CurrentYear}} {{AppName}}</p>
        </body></html>";
    }

    private static string GetFallbackWorkSpaceInvitationTemplate()
    {
        return @"<!DOCTYPE html><html><body>
            <h2>You're Invited</h2>
            <p>Hi {{recipientName}},</p>
            <p><strong>{{inviterName}}</strong> has invited you to join the <strong>{{workspaceName}}</strong> workspace on {{appName}}.</p>
            <p>Accept your invitation: <a href=""{{inviteUrl}}"">{{inviteUrl}}</a></p>
            <p>This invitation expires in 7 days.</p>
            <p>If you don't want to join, you can ignore this email.</p>
            <p>&copy; {{CurrentYear}} {{appName}}</p>
        </body></html>";
    }
}