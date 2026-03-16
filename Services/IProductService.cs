using PinoyPantry.API.DTOs;

namespace PinoyPantry.API.Services
{
    public interface IProductService
    {
        Task<PagedResult<ProductResponseDto>> GetAllProductsAsync(ProductQueryParams query);
        Task<ProductResponseDto?> GetProductByIdAsync(int id);
        Task<ProductResponseDto> CreateProductAsync(CreateProductDto productDto);
        Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto productDto);
        Task<bool> DeleteProductAsync(int id);
        Task<ProductResponseDto?> GetByIdAsync(int id);
        Task UpdateImageUrlAsync(int id, string imageUrl);
    }
}
