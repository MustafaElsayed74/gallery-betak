using GalleryBetak.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace GalleryBetak.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var smtpHost = _configuration["EmailConfiguration:SmtpServer"] ?? _configuration["SmtpSettings:Host"];
        var smtpPortRaw = _configuration["EmailConfiguration:Port"] ?? _configuration["SmtpSettings:Port"];
        var username = _configuration["EmailConfiguration:UserName"] ?? _configuration["SmtpSettings:Username"];
        var password = _configuration["EmailConfiguration:Password"] ?? _configuration["SmtpSettings:Password"];
        var from = _configuration["EmailConfiguration:From"] ?? _configuration["SmtpSettings:FromEmail"] ?? username;
        var fromName = _configuration["SmtpSettings:FromName"] ?? "GalleryBetak";

        if (string.IsNullOrWhiteSpace(smtpHost) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException("Email SMTP configuration is incomplete.");
        }

        var port = int.TryParse(smtpPortRaw, out var parsedPort) ? parsedPort : 587;
        var enableSsl = port == 465 || port == 587; // Usually 587 uses STARTTLS which is covered by EnableSsl in SmtpClient

        var fromAddress = new MailAddress(from, fromName);
        using var message = new MailMessage
        {
            From = fromAddress,
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(toEmail);

        using var smtp = new SmtpClient(smtpHost, port)
        {
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(username, password)
        };

        using var registration = cancellationToken.Register(() => smtp.SendAsyncCancel());
        
        try 
        {
            await smtp.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            throw;
        }
    }

    public async Task SendOrderTrackingEmailAsync(string toEmail, string orderNumber, string status, string? trackingNumber, CancellationToken cancellationToken = default)
    {
        var subject = $"تحديث حالة الطلب: {orderNumber}";
        
        var body = $"مرحباً،\n\nنود إعلامك بأن حالة طلبك رقم {orderNumber} قد تغيرت إلى: {status}.\n\n";
        
        if (!string.IsNullOrWhiteSpace(trackingNumber))
        {
            body += $"رقم التتبع الخاص بشحنتك هو: {trackingNumber}\n\n";
        }
        
        body += "شكراً لتسوقك معنا!\nمتجر Bloomi Store";

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }
}
