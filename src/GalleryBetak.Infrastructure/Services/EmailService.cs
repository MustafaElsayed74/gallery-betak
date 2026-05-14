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
        var fromName = _configuration["SmtpSettings:FromName"] ?? "جاليري بيتك";

        if (string.IsNullOrWhiteSpace(smtpHost) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(from))
        {
            _logger.LogWarning("Email SMTP configuration is incomplete — skipping email send.");
            return;
        }

        var port = int.TryParse(smtpPortRaw, out var parsedPort) ? parsedPort : 587;
        var enableSsl = port == 465 || port == 587;

        var fromAddress = new MailAddress(from, fromName);
        using var message = new MailMessage
        {
            From = fromAddress,
            Subject = subject,
            Body = body,
            IsBodyHtml = true
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

    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink, CancellationToken cancellationToken = default)
    {
        var subject = "إعادة تعيين كلمة المرور — جاليري بيتك";
        var body = BuildBrandedEmail(
            title: "إعادة تعيين كلمة المرور",
            greeting: $"مرحباً {userName}،",
            mainText: "تلقينا طلباً لإعادة تعيين كلمة المرور الخاصة بحسابك في جاليري بيتك. انقر على الزر أدناه لإنشاء كلمة مرور جديدة.",
            ctaLabel: "إعادة تعيين كلمة المرور",
            ctaUrl: resetLink,
            footnote: "إذا لم تطلب إعادة تعيين كلمة المرور، يمكنك تجاهل هذا البريد بأمان — حسابك بخير. الرابط صالح لمدة 24 ساعة فقط."
        );

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    public async Task SendEmailVerificationCodeAsync(string toEmail, string userName, string code, CancellationToken cancellationToken = default)
    {
        var subject = "تأكيد بريدك الإلكتروني — جاليري بيتك";
        var body = BuildBrandedEmail(
            title: "تأكيد البريد الإلكتروني",
            greeting: $"أهلاً {userName}،",
            mainText: "شكراً لانضمامك إلى جاليري بيتك! استخدم الكود التالي لتأكيد بريدك الإلكتروني:",
            ctaLabel: null,
            ctaUrl: null,
            otp: code,
            footnote: "الكود صالح لمدة محدودة. إذا لم تُنشئ حساباً في جاليري بيتك، يمكنك تجاهل هذا البريد."
        );

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    public async Task SendOrderTrackingEmailAsync(string toEmail, string orderNumber, string status, string? trackingNumber, CancellationToken cancellationToken = default)
    {
        var subject = $"تحديث حالة الطلب: {orderNumber} — جاليري بيتك";

        var mainText = $"نود إعلامك بأن حالة طلبك رقم <strong>{orderNumber}</strong> قد تغيّرت إلى: <strong>{status}</strong>.";
        if (!string.IsNullOrWhiteSpace(trackingNumber))
            mainText += $"<br><br>رقم التتبع الخاص بشحنتك: <strong>{trackingNumber}</strong>";

        var body = BuildBrandedEmail(
            title: $"تحديث الطلب #{orderNumber}",
            greeting: "عزيزنا العميل،",
            mainText: mainText,
            ctaLabel: "تتبع طلبك",
            ctaUrl: "https://gallery-betak.vercel.app/account",
            footnote: "شكراً لتسوقك مع جاليري بيتك. نحن دائماً هنا لخدمتك."
        );

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    // ── Branded HTML Email Builder ────────────────────────────────────
    private static string BuildBrandedEmail(
        string title,
        string greeting,
        string mainText,
        string? ctaLabel,
        string? ctaUrl,
        string? otp = null,
        string? footnote = null)
    {
        var ctaHtml = ctaLabel != null && ctaUrl != null
            ? $@"<div style=""text-align:center;margin:32px 0;"">
                    <a href=""{ctaUrl}"" style=""display:inline-block;background:linear-gradient(135deg,#0ea5e9,#14b8a6);color:#fff;text-decoration:none;font-size:16px;font-weight:700;padding:14px 40px;border-radius:12px;font-family:'Segoe UI',Tahoma,sans-serif;"">
                        {ctaLabel}
                    </a>
                 </div>"
            : string.Empty;

        var otpHtml = otp != null
            ? $@"<div style=""text-align:center;margin:28px 0;"">
                    <div style=""display:inline-block;background:#0f172a;border:2px solid #0ea5e9;border-radius:16px;padding:20px 40px;"">
                        <span style=""font-size:36px;font-weight:900;letter-spacing:12px;color:#0ea5e9;font-family:'Courier New',monospace;"">{otp}</span>
                    </div>
                 </div>"
            : string.Empty;

        var footnoteHtml = footnote != null
            ? $@"<p style=""font-size:12px;color:#94a3b8;margin-top:28px;line-height:1.6;direction:rtl;text-align:center;"">{footnote}</p>"
            : string.Empty;

        return $@"<!DOCTYPE html>
<html lang=""ar"" dir=""rtl"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width,initial-scale=1.0"">
<title>{title}</title>
</head>
<body style=""margin:0;padding:0;background:#0b1329;font-family:'Segoe UI',Tahoma,Arial,sans-serif;"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#0b1329;min-height:100vh;"">
<tr><td align=""center"" style=""padding:40px 16px;"">
<table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;width:100%;"">

  <!-- Header -->
  <tr>
    <td style=""background:linear-gradient(135deg,#0b1329 0%,#134e4a 50%,#0369a1 100%);border-radius:20px 20px 0 0;padding:40px 40px 32px;text-align:center;"">
      <div style=""display:inline-flex;align-items:center;gap:12px;margin-bottom:8px;"">
        <div style=""width:48px;height:48px;background:linear-gradient(135deg,#0ea5e9,#14b8a6);border-radius:12px;display:inline-flex;align-items:center;justify-content:center;"">
          <span style=""font-size:22px;"">🏠</span>
        </div>
        <span style=""font-size:26px;font-weight:900;color:#fff;letter-spacing:-0.5px;"">جاليري بيتك</span>
      </div>
      <p style=""color:rgba(186,230,253,0.8);font-size:13px;margin:0;"">وجهتك الأولى للأثاث والديكور المنزلي</p>
    </td>
  </tr>

  <!-- Body -->
  <tr>
    <td style=""background:#ffffff;padding:40px;"">
      <h1 style=""font-size:22px;font-weight:800;color:#0f172a;margin:0 0 16px;direction:rtl;text-align:right;"">{title}</h1>
      <p style=""font-size:15px;color:#334155;line-height:1.8;direction:rtl;text-align:right;margin:0 0 8px;"">{greeting}</p>
      <p style=""font-size:15px;color:#475569;line-height:1.8;direction:rtl;text-align:right;margin:0 0 24px;"">{mainText}</p>
      {otpHtml}
      {ctaHtml}
      {footnoteHtml}
    </td>
  </tr>

  <!-- Footer -->
  <tr>
    <td style=""background:#0f172a;border-radius:0 0 20px 20px;padding:24px 40px;text-align:center;"">
      <p style=""font-size:12px;color:#64748b;margin:0 0 6px;"">جاليري بيتك — مركز ديرب نجم، شارع السنترال</p>
      <p style=""font-size:12px;color:#64748b;margin:0;"">
        <a href=""https://gallery-betak.vercel.app"" style=""color:#0ea5e9;text-decoration:none;"">gallery-betak.vercel.app</a>
        &nbsp;|&nbsp;
        <a href=""tel:+201289095013"" style=""color:#0ea5e9;text-decoration:none;"">+20 128 909 5013</a>
      </p>
      <p style=""font-size:11px;color:#475569;margin:12px 0 0;"">© {DateTime.UtcNow.Year} جاليري بيتك. جميع الحقوق محفوظة.</p>
    </td>
  </tr>

</table>
</td></tr>
</table>
</body>
</html>";
    }
}
