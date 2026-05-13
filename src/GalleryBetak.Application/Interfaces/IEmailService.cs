using System.Threading;
using System.Threading.Tasks;

namespace GalleryBetak.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
    Task SendOrderTrackingEmailAsync(string toEmail, string orderNumber, string status, string? trackingNumber, CancellationToken cancellationToken = default);
}
