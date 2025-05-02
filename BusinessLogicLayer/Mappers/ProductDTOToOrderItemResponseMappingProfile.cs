using AutoMapper;

using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Mappers;

public class ProductDTOToOrderItemResponseMappingProfile : Profile
{
    public ProductDTOToOrderItemResponseMappingProfile()
    {
        // We are mapping ProductName, Category, and UnitPrice from ProductDTO to OrderItemResponse
        CreateMap<ProductDTO, OrderItemResponse>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductName))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice))
            .ForMember(dest => dest.Quantity, opt => opt.Ignore())
            .ForMember(dest => dest.ProductID, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore());
    }
}