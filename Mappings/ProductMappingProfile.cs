using AutoMapper;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;
using PinoyPantry.API.Services;

namespace PinoyPantry.API.Mappings
{
    public class ProductMappingProfile : Profile
    {
        public ProductMappingProfile()
        {
            // All field names now match — no .ForMember() needed
            CreateMap<Product, ProductResponseDto>();
            CreateMap<Product, AdminProductResponseDto>()
                .ForMember(dest => dest.ProfitAmount, opt => opt.MapFrom(src => PricingCalculator.Breakdown(src.Price, src.CostPrice).ProfitAmount))
                .ForMember(dest => dest.GstAmount, opt => opt.MapFrom(src => PricingCalculator.Breakdown(src.Price, src.CostPrice).GstAmount))
                .ForMember(dest => dest.GstRate, opt => opt.MapFrom(src => PricingCalculator.GstRate));
            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>();
            CreateMap<ImportProductDto, Product>();
        }
    }
}
