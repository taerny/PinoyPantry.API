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
                .ForMember(dest => dest.GstRate, opt => opt.MapFrom(src => PricingCalculator.GstRate))
                // The frontend only shows this when it differs from CostPrice — always mapping
                // the raw "most recent batch" cost here keeps that decision out of AutoMapper.
                .ForMember(dest => dest.LatestBatchCostPrice, opt => opt.MapFrom(src =>
                    src.Batches.Any()
                        ? src.Batches.OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id).First().CostPrice
                        : (decimal?)null));
            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>();

            // StockQuantity on the DTO represents the quantity being received in this import —
            // it becomes the product's initial batch (see ProductService.ImportProductsAsync),
            // not a direct write to Product.StockQuantity.
            CreateMap<ImportProductDto, Product>()
                .ForMember(dest => dest.StockQuantity, opt => opt.Ignore());
        }
    }
}
