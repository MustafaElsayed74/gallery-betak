using ElMasria.Domain.Entities;
using ElMasria.Domain.Specifications;

namespace ElMasria.Application.Specifications;

/// <summary>
/// Retrieves a Payment by its Gateway Order ID.
/// </summary>
public sealed class PaymentByGatewayOrderIdSpecification : BaseSpecification<Payment>
{
    public PaymentByGatewayOrderIdSpecification(string gatewayOrderId) 
        : base(p => p.GatewayOrderId == gatewayOrderId)
    {
    }
}
