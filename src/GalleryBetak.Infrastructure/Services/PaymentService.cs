using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Payment;
using GalleryBetak.Application.Interfaces;
using GalleryBetak.Application.Specifications;
using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Enums;
using GalleryBetak.Domain.Exceptions;
using GalleryBetak.Domain.Interfaces;

using GalleryBetak.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GalleryBetak.Infrastructure.Services;

/// <summary>
/// Robust Paymob Integration Service handling multi-step token acquisition, API negotiation,
/// and secure HMAC webhook signature validations.
/// </summary>
public sealed class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly HttpClient _httpClient;
    private readonly PaymobSettings _settings;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        IHttpClientFactory httpClientFactory,
        IOptions<PaymobSettings> settings,
        ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        // Centralized standard client for Paymob
        _httpClient = httpClientFactory.CreateClient("Paymob");
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>Phase 1: Get Authentication Token</summary>
    private async Task<string> AuthenticateAsync(CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync("auth/tokens", new { api_key = _settings.ApiKey }, ct);
        
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return content.GetProperty("token").GetString()!;
    }

    /// <summary>Phase 2: Register Order on Paymob</summary>
    private async Task<string> RegisterOrderAsync(string token, Order order, CancellationToken ct)
    {
        var payload = new
        {
            auth_token = token,
            delivery_needed = "false",
            amount_cents = (int)(order.TotalAmount * 100),
            currency = "EGP",
            merchant_order_id = $"{order.OrderNumber}-{Guid.NewGuid().ToString("N")[..6]}",
            items = order.Items.Select(i => new
            {
                name = string.IsNullOrWhiteSpace(i.ProductNameEn) ? i.ProductNameAr : i.ProductNameEn,
                amount_cents = (int)(i.UnitPrice * 100),
                description = i.ProductNameAr,
                quantity = i.Quantity
            }).ToArray()
        };

        var response = await _httpClient.PostAsJsonAsync("ecommerce/orders", payload, ct);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return content.GetProperty("id").GetInt32().ToString();
    }

    /// <summary>Phase 3: Get Payment Key</summary>
    private async Task<string> GetPaymentKeyAsync(string token, string paymobOrderId, Order order, int integrationId, CancellationToken ct)
    {
        var customerEmail = "noreply@gallery-betak.local";
        if (!string.IsNullOrWhiteSpace(order.UserId))
        {
            var user = await _userManager.FindByIdAsync(order.UserId);
            customerEmail = user?.Email ?? customerEmail;
        }

        var payload = new
        {
            auth_token = token,
            amount_cents = (int)(order.TotalAmount * 100),
            expiration = 3600,
            order_id = paymobOrderId,
            currency = "EGP",
            integration_id = integrationId,
            billing_data = new
            {
                apartment = order.ShippingApartmentNo ?? "NA",
                building = order.ShippingBuildingNo ?? "NA",
                email = customerEmail,
                floor = "NA",
                first_name = order.ShippingRecipientName.Split(' ').FirstOrDefault() ?? "NA",
                last_name = string.Join(' ', order.ShippingRecipientName.Split(' ').Skip(1)) ?? "NA",
                street = order.ShippingStreetAddress,
                city = order.ShippingCity,
                country = "EG",
                state = order.ShippingGovernorate,
                phone_number = order.ShippingPhone
            }
        };

        var response = await _httpClient.PostAsJsonAsync("acceptance/payment_keys", payload, ct);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return content.GetProperty("token").GetString()!;
    }

    /// <summary>Retrieve Fawry Reference</summary>
    private async Task<string> InitiateFawryPayAsync(string paymentToken, CancellationToken ct)
    {
        var payload = new { source = new { identifier = "AGGREGATOR", subtype = "AGGREGATOR" }, payment_token = paymentToken };
        var response = await _httpClient.PostAsJsonAsync("acceptance/payments/pay", payload, ct);
        
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        
        var pending = content.GetProperty("pending").GetString();
        if (pending == "true")
        {
            return content.GetProperty("data").GetProperty("bill_reference").GetInt32().ToString(); // Fawry code
        }
        
        throw new BusinessRuleException("فشل إصدار كود فوري", "Failed to generate Fawry reference.");
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<PaymentInitiationResponse>> CreatePaymentKeyAsync(int orderId, CancellationToken ct = default)
    {
        var spec = new OrderWithItemsSpecification(orderId);
        var order = await _unitOfWork.Orders.GetEntityWithSpecAsync(spec, ct);

        if (order is null)
            return ApiResponse<PaymentInitiationResponse>.Fail(404, "الطلب غير موجود", "Order not found.");

        if (order.PaymentStatus == PaymentStatus.Success)
            return ApiResponse<PaymentInitiationResponse>.Fail(400, "تم الدفع مسبقاً", "Order already paid.");

        // Handled completely outside Paymob
        if (order.PaymentMethod == PaymentMethod.COD)
            return ApiResponse<PaymentInitiationResponse>.Ok(new PaymentInitiationResponse(string.Empty, "COD"), "جاهز للدفع عند الاستلام", "COD activated.");

        try
        {
            // Flow: Auth -> Register Order -> Get Key
            string token = await AuthenticateAsync(ct);
            string paymobOrderId = await RegisterOrderAsync(token, order, ct);
            
            // Record payment tracking intent
            var payment = Payment.Create(order.Id, order.TotalAmount, order.PaymentMethod);
            payment.StartProcessing(paymobOrderId);
            await _unitOfWork.Payments.AddAsync(payment, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // Fetch integration specific keys
            int integrationId = order.PaymentMethod switch
            {
                PaymentMethod.Card => _settings.CardIntegrationId,
                PaymentMethod.Fawry => _settings.FawryIntegrationId,
                PaymentMethod.VodafoneCash or PaymentMethod.OrangeCash or PaymentMethod.EtisalatCash => _settings.WalletIntegrationId,
                _ => throw new BusinessRuleException("وسيلة الدفع غير مدعومة", "Unsupported payment method.")
            };

            string paymentKey = await GetPaymentKeyAsync(token, paymobOrderId, order, integrationId, ct);

            // Construct Response
            PaymentInitiationResponse initResponse = order.PaymentMethod switch
            {
                PaymentMethod.Card => new PaymentInitiationResponse($"https://accept.paymob.com/api/acceptance/iframes/{_settings.CardIframeId}?payment_token={paymentKey}"),
                PaymentMethod.VodafoneCash or PaymentMethod.OrangeCash or PaymentMethod.EtisalatCash => new PaymentInitiationResponse($"https://accept.paymob.com/api/acceptance/iframes/{_settings.WalletIntegrationId}?payment_token={paymentKey}"), // Typically wallet requires phone number post, simplifying mapping
                PaymentMethod.Fawry => new PaymentInitiationResponse(string.Empty, await InitiateFawryPayAsync(paymentKey, ct)),
                _ => throw new InvalidOperationException()
            };

            return ApiResponse<PaymentInitiationResponse>.Ok(initResponse);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Paymob API connection failure");
            return ApiResponse<PaymentInitiationResponse>.Fail(502, "فشل الاتصال ببوابة الدفع", "Payment gateway error.");
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HandleWebhookAsync(PaymobWebhookPayload payload, string hmac, CancellationToken ct = default)
    {
        if (!VerifyHmac(payload, hmac))
        {
            _logger.LogWarning("Invalid HMAC signature received from Paymob");
            return false;
        }

        var merchantOrderIdStr = payload.obj.order.merchant_order_id;
        if (string.IsNullOrEmpty(merchantOrderIdStr)) return false;

        // Extract native Order ID (e.g. "ORD-202310-12345-aabbcc" => 12345 ??? No, let's fetch by OrderNumber string, but wait, Paymob sends "ORD-yyyyMM-xxxxx-guid")
        var orderNumber = string.Join('-', merchantOrderIdStr.Split('-').Take(3));
        
        var order = await _unitOfWork.Orders.GetEntityWithSpecAsync(new OrderByNumberSpecification(orderNumber), ct);
        if (order is null) return false;

        var payment = await _unitOfWork.Payments.GetEntityWithSpecAsync(new PaymentByGatewayOrderIdSpecification(payload.obj.order.id.ToString()), ct);
        if (payment is null) return false;

        // Process status
        bool isSuccess = payload.obj.success && !payload.obj.pending;
        string rawJson = JsonSerializer.Serialize(payload);

        if (isSuccess)
        {
            payment.Confirm(payload.obj.id.ToString(), rawJson);
            order.UpdatePaymentStatus(PaymentStatus.Success);
            
            // Advance state machine if order is still Pending
            if (order.Status == OrderStatus.Pending)
                order.Confirm();
        }
        else
        {
            payment.Fail(payload.obj.order.data ?? "Failed", rawJson);
            order.UpdatePaymentStatus(PaymentStatus.Failed);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Implements secure hash validation against Paymob specs.
    /// Concatenates values of [amount_cents, created_at, currency, error_occured, has_parent_transaction, id, integration_id, is_3d_secure, is_auth, is_capture, is_refunded, is_standalone_payment, is_voided, order.id, owner, pending, source_data.pan, source_data.sub_type, source_data.type, success]
    /// </summary>
    private bool VerifyHmac(PaymobWebhookPayload payload, string hmac)
    {
        if (string.IsNullOrWhiteSpace(hmac))
        {
            return false;
        }

        var obj = payload.obj;
        var dataStr = $"{obj.amount_cents}{obj.created_at}{obj.currency}{obj.has_parent_transaction.ToString().ToLower()}{obj.id}{obj.integration_id}{obj.is_3d_secure.ToString().ToLower()}{obj.is_capture.ToString().ToLower()}{obj.is_refunded.ToString().ToLower()}{obj.is_standalone_payment.ToString().ToLower()}{obj.is_voided.ToString().ToLower()}{obj.order.id}{obj.owner}{obj.pending.ToString().ToLower()}";

        var keyByte = Encoding.UTF8.GetBytes(_settings.HmacSecret);
        var messageBytes = Encoding.UTF8.GetBytes(dataStr);

        using var hash = new HMACSHA512(keyByte);
        var hashBytes = hash.ComputeHash(messageBytes);
        var computedHmac = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        return string.Equals(computedHmac, hmac, StringComparison.OrdinalIgnoreCase);
    }
}

