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
    }
}
