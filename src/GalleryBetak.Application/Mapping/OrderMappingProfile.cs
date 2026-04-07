using AutoMapper;
using GalleryBetak.Application.DTOs.Order;
using DomainOrder = GalleryBetak.Domain.Entities.Order;
using DomainOrderItem = GalleryBetak.Domain.Entities.OrderItem;

namespace GalleryBetak.Application.Mapping;

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

