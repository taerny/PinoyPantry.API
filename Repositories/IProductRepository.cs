using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Repositories
{
    public interface IProductRepository
    {
        Task<(IEnumerable<Product> Products, int TotalCount)> GetAllProductsAsync(ProductQueryParams query);
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product> CreateProductAsync(Product product);
        Task<Product?> UpdateProductAsync(int id, Product product);
        Task<bool> DeleteProductAsync(int id);
        Task UpdateImageUrlAsync(int id, string imageUrl);
        Task<int> DeleteAllProductsAsync();
        Task<Dictionary<string, int>> GetCategoryCountsAsync();
        Task<int> ImportProductsAsync(IEnumerable<Product> products);
        Task<List<Product>> GetAllRawAsync();
        Task<Product?> UpdatePricingAsync(int id, decimal costPrice, decimal? recommendedRetail, decimal? margin);
        Task SetCodeAsync(int id, string code);

        // Creates "Batch 1" for a freshly-imported product and sets its StockQuantity to
        // match — the equivalent of what the AddProductBatches migration did to backfill
        // products that already existed before batch tracking began.
        Task CreateInitialBatchAsync(int productId, int quantity);

        // Which of these codes already belong to an existing product — used by the PDF import
        // preview to flag likely duplicates before the admin confirms anything.
        Task<HashSet<string>> GetExistingCodesAsync(IEnumerable<string> codes);
    }
}
