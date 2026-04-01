using AutoMapper;
using ElMasria.Application.DTOs.Order;
using DomainOrder = ElMasria.Domain.Entities.Order;
using DomainOrderItem = ElMasria.Domain.Entities.OrderItem;

namespace ElMasria.Application.Mapping;

/// <summary>
/// Mapping configurations for Orders and OrderItems.
/// </summary>
public sealed class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<DomainOrder, OrderDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.PaymentMethod, opt => opt.MapFrom(s => s.PaymentMethod.ToString()))
            .ForMember(d => d.PaymentStatus, opt => opt.MapFrom(s => s.PaymentStatus.ToString()));

        CreateMap<DomainOrder, OrderSummaryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.ItemCount, opt => opt.MapFrom(s => s.Items.Sum(i => i.Quantity)));

        CreateMap<DomainOrderItem, OrderItemDto>();
    }
}
