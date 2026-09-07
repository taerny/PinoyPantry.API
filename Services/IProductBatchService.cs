using PinoyPantry.API.DTOs;

namespace PinoyPantry.API.Services
{
    public interface IProductBatchService
    {
        Task<ProductBatchListResponseDto?> GetBatchesAsync(int productId);
        Task<(ProductBatchDto? Batch, string? Error)> CreateBatchAsync(int productId, CreateProductBatchDto dto);
        Task<bool?> DeleteBatchAsync(int productId, int batchId);
    }
}
