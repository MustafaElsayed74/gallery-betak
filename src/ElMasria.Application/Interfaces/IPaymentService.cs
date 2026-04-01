using ElMasria.Application.Common;
using ElMasria.Application.DTOs.Payment;

namespace ElMasria.Application.Interfaces;

/// <summary>
/// Service contract for Paymob integration and webhook processing.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Generates a payment initialization response (e.g. IFrame URL for Card, Reference Code for Fawry).
    /// </summary>
    Task<ApiResponse<PaymentInitiationResponse>> CreatePaymentKeyAsync(int orderId, CancellationToken ct = default);

    /// <summary>
    /// Processes Paymob transaction webhooks and safely transitions the Order state machine.
    /// </summary>
    Task<bool> HandleWebhookAsync(PaymobWebhookPayload payload, string hmac, CancellationToken ct = default);
}
