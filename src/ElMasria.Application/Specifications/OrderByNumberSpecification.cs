using ElMasria.Domain.Entities;
using ElMasria.Domain.Specifications;

namespace ElMasria.Application.Specifications;

/// <summary>
/// Retrieves an Order by its unique OrderNumber.
/// </summary>
public sealed class OrderByNumberSpecification : BaseSpecification<Order>
{
    public OrderByNumberSpecification(string orderNumber) 
        : base(o => o.OrderNumber == orderNumber)
    {
    }
}
