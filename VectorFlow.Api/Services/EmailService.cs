using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Resend;
using VectorFlow.Api.Services.Interfaces;

namespace VectorFlow.Api.Services;

public class EmailService(IConfiguration configuration, IResend resend) : IEmailService
{
    public async Task SendEmailAsync(string recipient, string subject, string htmlBody)
    {
        var env = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");

        if (env == "Production")
            await SendEmailPrdAsync(recipient, subject, htmlBody);
        else
            await SendEmailDevAsync(recipient, subject, htmlBody);
    }

    /// <summary>
    /// Development — sends via SMTP (e.g. Gmail, Mailpit, or Mailtrap).
    /// Configure via .env:
    ///   EmailConf__Email=you@gmail.com
    ///   EmailConf__Password=your-app-password
    ///   EmailConf__Host=smtp.gmail.com
    ///   EmailConf__Port=587
    /// </summary>
    private async Task SendEmailDevAsync(string recipient, string subject, string htmlBody)
    {
        var email = configuration["EmailConf:Email"]
            ?? throw new InvalidOperationException("EmailConf:Email not configured.");
        var password = configuration["EmailConf:Password"]
            ?? throw new InvalidOperationException("EmailConf:Password not configured.");
        var host = configuration["EmailConf:Host"]
            ?? throw new InvalidOperationException("EmailConf:Host not configured.");
        var port = configuration.GetValue<int>("EmailConf:Port");

        using var smtpClient = new SmtpClient(host, port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(email, password)
        };

        var message = new MailMessage
        {
            From = new MailAddress(email, "VectorFlow"),
            Subject = subject
        };

        message.To.Add(recipient);
        message.AlternateViews.Add(
            AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html)
        );

        await smtpClient.SendMailAsync(message);
    }

    /// <summary>
    /// Production — sends via Resend.
    /// Configure via .env:
    ///   EmailConf__From=noreply@yourdomain.com
    ///   RESEND_API_KEY=re_xxxxxxxxxxxx  (picked up automatically by the Resend SDK)
    /// </summary>
    private async Task SendEmailPrdAsync(string recipient, string subject, string htmlBody)
    {
        var from = configuration["EmailConf:From"]
            ?? throw new InvalidOperationException("EmailConf:From not configured.");

        var message = new EmailMessage
        {
            From = from,
            To = new EmailAddressList { recipient },
            Subject = subject,
            HtmlBody = htmlBody
        };

        await resend.EmailSendAsync(message);
    }
}