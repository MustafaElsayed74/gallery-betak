using ElMasria.Domain.Enums;
using ElMasria.Domain.Events;

namespace ElMasria.Domain.Entities;

/// <summary>
/// Payment transaction linked to an order. Supports multiple payment attempts
/// and stores full gateway response for audit.
/// </summary>
public sealed class Payment : BaseEntity
{
    /// <summary>Order ID.</summary>
    public int OrderId { get; private set; }

    /// <summary>Paymob transaction ID.</summary>
    public string? TransactionId { get; private set; }

    /// <summary>Paymob order ID.</summary>
    public string? GatewayOrderId { get; private set; }

    /// <summary>Payment amount in EGP.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Currency code (always EGP).</summary>
    public string Currency { get; private set; } = "EGP";

    /// <summary>Payment method used.</summary>
    public PaymentMethod Method { get; private set; }

    /// <summary>Payment status.</summary>
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;

    /// <summary>Full gateway JSON response for audit.</summary>
    public string? GatewayResponse { get; private set; }

    /// <summary>When payment was confirmed.</summary>
    public DateTime? PaidAt { get; private set; }

    /// <summary>Failure reason from gateway.</summary>
    public string? FailureReason { get; private set; }

    /// <summary>When refund was processed.</summary>
    public DateTime? RefundedAt { get; private set; }

    /// <summary>Refund amount (may be partial).</summary>
    public decimal? RefundAmount { get; private set; }

    /// <summary>Last update timestamp.</summary>
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    /// <summary>The order.</summary>
    public Order Order { get; private set; } = null!;

    // Domain events
    private readonly List<BaseDomainEvent> _domainEvents = new();
    /// <summary>Pending domain events.</summary>
    public IReadOnlyList<BaseDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    /// <summary>Clears domain events.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    private Payment() { }

    /// <summary>Creates a payment record.</summary>
    public static Payment Create(int orderId, decimal amount, PaymentMethod method)
    {
        if (amount <= 0)
            throw new Exceptions.DomainException("مبلغ الدفع يجب أن يكون أكبر من صفر", "Payment amount must be > 0.");

        return new Payment
        {
            OrderId = orderId,
            Amount = amount,
            Method = method
        };
    }

    /// <summary>Marks payment as processing (gateway initiated).</summary>
    public void StartProcessing(string gatewayOrderId)
    {
        Status = PaymentStatus.Processing;
        GatewayOrderId = gatewayOrderId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Confirms payment success from gateway callback.</summary>
    public void Confirm(string transactionId, string gatewayResponse)
    {
        Status = PaymentStatus.Success;
        TransactionId = transactionId;
        GatewayResponse = gatewayResponse;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        _domainEvents.Add(new PaymentConfirmedEvent(OrderId, Amount, transactionId));
    }

    /// <summary>Records payment failure.</summary>
    public void Fail(string reason, string? gatewayResponse = null)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        GatewayResponse = gatewayResponse;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Processes a refund.</summary>
    public void Refund(decimal refundAmount)
    {
        if (Status != PaymentStatus.Success)
            throw new Exceptions.BusinessRuleException("لا يمكن استرجاع مبلغ لعملية غير ناجحة", "Can only refund successful payments.");
        if (refundAmount > Amount)
            throw new Exceptions.BusinessRuleException("مبلغ الاسترجاع أكبر من المبلغ المدفوع", "Refund amount exceeds payment.");

        Status = PaymentStatus.Refunded;
        RefundAmount = refundAmount;
        RefundedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
