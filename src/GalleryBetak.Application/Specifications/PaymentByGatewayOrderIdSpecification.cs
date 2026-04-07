using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Specifications;

namespace GalleryBetak.Application.Specifications;

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

