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
            Subject = "Добре дошъл/а в Momentix! / Welcome to Momentix!",
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
        var name = string.IsNullOrWhiteSpace(fullName) ? "потребителю" : fullName.Trim();
        return $"""
        Здравей, {name}!

        Добре дошъл/а в Momentix — мястото, където съхраняваш своите моменти.

        Акаунтът ти беше създаден успешно. Вече можеш да влезеш и да започнеш да създаваш албуми, да споделяш спомени с приятели и да улавяш специалните моменти от живота си.

        Ако не си се регистрирал/а в Momentix, можеш да игнорираш този имейл.

        С уважение,
        Екипът на Momentix

        ────────────────────────────────────

        Hi, {name}!

        Welcome to Momentix — the place where you preserve your moments.

        Your account has been created successfully. You can now log in and start creating albums, sharing memories with friends, and capturing the special moments in your life.

        If you did not register for Momentix, you can ignore this email.

        Best regards,
        The Momentix Team
        """;
    }
}
