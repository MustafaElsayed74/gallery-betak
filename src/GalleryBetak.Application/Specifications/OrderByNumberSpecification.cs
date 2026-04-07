using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Specifications;

namespace GalleryBetak.Application.Specifications;

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

