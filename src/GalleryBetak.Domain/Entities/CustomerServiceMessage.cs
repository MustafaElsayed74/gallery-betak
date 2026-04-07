using GalleryBetak.Domain.Enums;
using GalleryBetak.Domain.Exceptions;

namespace GalleryBetak.Domain.Entities;

/// <summary>
/// Customer service message submitted from storefront users or guests.
/// </summary>
public sealed class CustomerServiceMessage : BaseEntity
{
    /// <summary>Sender display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Sender email address.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Sender phone number if provided.</summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>Short issue subject.</summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>Full customer message body.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Current handling status.</summary>
    public CustomerServiceMessageStatus Status { get; private set; } = CustomerServiceMessageStatus.New;

    /// <summary>Internal admin notes for resolution history.</summary>
    public string? AdminNotes { get; private set; }

    /// <summary>User id of admin/agent who handled the message last.</summary>
    public string? HandledByUserId { get; private set; }

    /// <summary>UTC timestamp when message reached resolved/closed state.</summary>
    public DateTime? ResolvedAt { get; private set; }

    private CustomerServiceMessage() { }

    /// <summary>Creates a new customer service message.</summary>
    public static CustomerServiceMessage Create(string name, string email, string? phoneNumber, string subject, string message)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("الاسم مطلوب", "Name is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("البريد الإلكتروني مطلوب", "Email is required.");
        if (string.IsNullOrWhiteSpace(subject))
            throw new DomainException("عنوان الرسالة مطلوب", "Subject is required.");
        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("محتوى الرسالة مطلوب", "Message body is required.");

        return new CustomerServiceMessage
        {
            Name = name.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
            Subject = subject.Trim(),
            Message = message.Trim(),
            Status = CustomerServiceMessageStatus.New
        };
    }

    /// <summary>Updates handling status and optional notes.</summary>
    public void UpdateStatus(CustomerServiceMessageStatus status, string? adminNotes, string? handledByUserId)
    {
        Status = status;
        AdminNotes = string.IsNullOrWhiteSpace(adminNotes) ? null : adminNotes.Trim();
        HandledByUserId = handledByUserId;

        if (status is CustomerServiceMessageStatus.Resolved or CustomerServiceMessageStatus.Closed)
        {
            ResolvedAt ??= DateTime.UtcNow;
        }
        else
        {
            ResolvedAt = null;
        }
    }
}
