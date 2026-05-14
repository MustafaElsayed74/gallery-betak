using System.Threading;
using System.Threading.Tasks;

namespace GalleryBetak.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
    Task SendOrderTrackingEmailAsync(string toEmail, string orderNumber, string status, string? trackingNumber, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink, CancellationToken cancellationToken = default);
    Task SendEmailVerificationCodeAsync(string toEmail, string userName, string code, CancellationToken cancellationToken = default);
}
