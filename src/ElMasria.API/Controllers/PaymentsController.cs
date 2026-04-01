using ElMasria.Application.Common;
using ElMasria.Application.DTOs.Payment;
using ElMasria.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElMasria.API.Controllers;

/// <summary>
/// Handles Payment logic and Webhooks.
/// </summary>
public class PaymentsController : BaseApiController
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>Generates the Payment Interface URL or Code for an order.</summary>
    /// <response code="200">Returns an IFrame URL or Fawry Reference Code.</response>
    [HttpPost("{orderId:int}/initiate")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PaymentInitiationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitiatePayment(int orderId)
    {
        var result = await _paymentService.CreatePaymentKeyAsync(orderId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Paymob Webhook Callbacks. Handles transactions automatically.</summary>
    /// <response code="200">Always returns 200 OK to the gateway.</response>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymobWebhook([FromQuery] string hmac, [FromBody] PaymobWebhookPayload payload)
    {
        if (payload.type == "TRANSACTION")
        {
            await _paymentService.HandleWebhookAsync(payload, hmac);
        }
        
        // Always respond 200 so the gateway knows we received it
        return Ok();
    }
}
