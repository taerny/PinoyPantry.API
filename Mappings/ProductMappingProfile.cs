using AutoMapper;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Mappings
{
    public class ProductMappingProfile : Profile
    {
        public ProductMappingProfile()
        {
            // All field names now match — no .ForMember() needed
            CreateMap<Product, ProductResponseDto>();
            CreateMap<Product, AdminProductResponseDto>();
            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>();
            CreateMap<ImportProductDto, Product>();
        }
    }
}
