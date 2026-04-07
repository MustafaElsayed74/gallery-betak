namespace GalleryBetak.Application.DTOs.Payment;

/// <summary>Response from payment initialization (IFrame URL or Fawry Code).</summary>
public sealed record PaymentInitiationResponse(string RedirectUrl, string? ReferenceCode = null);

/// <summary>Raw Paymob Webhook Payload.</summary>
public sealed class PaymobWebhookPayload
{
    public string type { get; set; } = string.Empty;
    public PaymobWebhookObj obj { get; set; } = new();
}

public sealed class PaymobWebhookObj
{
    public int id { get; set; }
    public bool pending { get; set; }
    public int amount_cents { get; set; }
    public bool success { get; set; }
    public bool is_voided { get; set; }
    public bool is_refunded { get; set; }
    public bool is_3d_secure { get; set; }
    public int integration_id { get; set; }
    public int profile_id { get; set; }
    public bool has_parent_transaction { get; set; }
    public OrderData order { get; set; } = new();
    public string created_at { get; set; } = string.Empty;
    public string currency { get; set; } = string.Empty;
    public string? terminal_receipt_number { get; set; }
    public bool is_void { get; set; }
    public bool is_capture { get; set; }
    public bool is_standalone_payment { get; set; }
    public bool is_refunded_core_valid { get; set; }
    public bool is_capture_core_valid { get; set; }
    public bool is_void_core_valid { get; set; }
    public bool refunded_amount_cents { get; set; }
    public bool captured_amount { get; set; }
    public string? merchant_staff_tag { get; set; }
    public int owner { get; set; }
    public string? parent_transaction { get; set; }
}

public sealed class OrderData
{
    public int id { get; set; }
    public string created_at { get; set; } = string.Empty;
    public bool delivery_needed { get; set; }
    public MerchantData merchant { get; set; } = new();
    public string? collector { get; set; }
    public int amount_cents { get; set; }
    public ShippingData? shipping_data { get; set; }
    public string currency { get; set; } = string.Empty;
    public bool is_payment_locked { get; set; }
    public bool is_return { get; set; }
    public bool is_cancel { get; set; }
    public bool is_returned { get; set; }
    public bool is_canceled { get; set; }
    public string? merchant_order_id { get; set; }
    public string? wallet_notification { get; set; }
    public int paid_amount_cents { get; set; }
    public bool notify_user_with_email { get; set; }
    public List<object> items { get; set; } = new();
    public string order_url { get; set; } = string.Empty;
    public int commission_fees { get; set; }
    public int delivery_fees_cents { get; set; }
    public int delivery_vat_cents { get; set; }
    public string payment_method { get; set; } = string.Empty;
    public string? merchant_staff_tag { get; set; }
    public string api_source { get; set; } = string.Empty;
    public string? data { get; set; }
}

public sealed class MerchantData
{
    public int id { get; set; }
    public string created_at { get; set; } = string.Empty;
    public List<string> phones { get; set; } = new();
    public List<string> company_emails { get; set; } = new();
    public string company_name { get; set; } = string.Empty;
    public string state { get; set; } = string.Empty;
    public string country { get; set; } = string.Empty;
    public string city { get; set; } = string.Empty;
    public string postal_code { get; set; } = string.Empty;
    public string street { get; set; } = string.Empty;
}

public sealed class ShippingData
{
    public int id { get; set; }
    public string first_name { get; set; } = string.Empty;
    public string last_name { get; set; } = string.Empty;
    public string street { get; set; } = string.Empty;
    public string building { get; set; } = string.Empty;
    public string floor { get; set; } = string.Empty;
    public string apartment { get; set; } = string.Empty;
    public string city { get; set; } = string.Empty;
    public string state { get; set; } = string.Empty;
    public string country { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string phone_number { get; set; } = string.Empty;
    public string postal_code { get; set; } = string.Empty;
    public string extra_description { get; set; } = string.Empty;
    public string? shipping_method { get; set; }
    public int order_id { get; set; }
    public int order { get; set; }
}

