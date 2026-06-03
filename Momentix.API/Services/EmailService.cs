using System.Net;
using System.Net.Mail;

namespace Momentix.API.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendRegistrationEmailAsync(string recipientEmail, string fullName)
    {
        var smtpEmail = Environment.GetEnvironmentVariable("MOMENTIX_SMTP_EMAIL")
            ?? _configuration["GmailSmtp:Email"];
        var smtpPassword = Environment.GetEnvironmentVariable("MOMENTIX_SMTP_PASSWORD")
            ?? _configuration["GmailSmtp:Password"];
        var host = Environment.GetEnvironmentVariable("MOMENTIX_SMTP_HOST")
            ?? _configuration["GmailSmtp:Host"]
            ?? "smtp.gmail.com";
        var portText = Environment.GetEnvironmentVariable("MOMENTIX_SMTP_PORT")
            ?? _configuration["GmailSmtp:Port"];
        var fromName = Environment.GetEnvironmentVariable("MOMENTIX_SMTP_FROM_NAME")
            ?? _configuration["GmailSmtp:FromName"]
            ?? "Momentix";

        if (string.IsNullOrWhiteSpace(smtpEmail) || string.IsNullOrWhiteSpace(smtpPassword))
            throw new InvalidOperationException("Gmail SMTP is not configured. Set MOMENTIX_SMTP_EMAIL and MOMENTIX_SMTP_PASSWORD.");

        if (!int.TryParse(portText, out var port))
            port = 587;

        using var message = new MailMessage
        {
            From = new MailAddress(smtpEmail, fromName),
            Subject = "Momentix registration email test",
            Body = BuildRegistrationBody(fullName),
            IsBodyHtml = false
        };

        message.To.Add(new MailAddress(recipientEmail));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(smtpEmail, smtpPassword)
        };

        _logger.LogInformation("Sending registration email to {RecipientEmail} through {Host}:{Port}.", recipientEmail, host, port);
        await client.SendMailAsync(message);
    }

    private static string BuildRegistrationBody(string fullName)
    {
        var name = string.IsNullOrWhiteSpace(fullName) ? "there" : fullName.Trim();
        return $"Hi {name},\n\nThis is a Momentix registration email test.\n\nIf you received this, Gmail SMTP is configured correctly.\n\nMomentix";
    }
}
