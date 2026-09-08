using Microsoft.EntityFrameworkCore;
using PinoyPantry.API.Data;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDBContext _context;

        public ProductRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Product> Products, int TotalCount)> GetAllProductsAsync(ProductQueryParams query)
        {
            var products = _context.Products.AsQueryable();

            // Batches are only needed for the admin listing (LatestBatchCostPrice) — the public
            // storefront listing has no use for them, so skip the extra join there.
            if (query.IncludeUnpublished)
                products = products.Include(p => p.Batches);

            if (!query.IncludeUnpublished)
                products = products.Where(p => p.IsPublished);

            if (!string.IsNullOrWhiteSpace(query.Category))
                products = products.Where(p => p.Category == query.Category);

            if (!string.IsNullOrWhiteSpace(query.Search))
                products = products.Where(p => p.Name.Contains(query.Search) || p.Description.Contains(query.Search));

            var totalCount = await products.CountAsync();

            var paged = await products
                .OrderBy(p => p.Id)
                .Skip((query.Page - 1) * query.Limit)
                .Take(query.Limit)
                .ToListAsync();

            return (paged, totalCount);
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Product?> UpdateProductAsync(int id, Product product)
        {
            var existing = await _context.Products.FindAsync(id);
            if (existing == null)
                return null;

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.Price = product.Price;
            existing.CostPrice = product.CostPrice;
            existing.ImageUrl = product.ImageUrl;
            existing.Category = product.Category;
            existing.StockQuantity = product.StockQuantity;
            existing.IsPublished = product.IsPublished;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task UpdateImageUrlAsync(int id, string imageUrl)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.ImageUrl = imageUrl;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> DeleteAllProductsAsync()
        {
            var count = await _context.Products.CountAsync();
            _context.Products.RemoveRange(_context.Products);
            await _context.SaveChangesAsync();
            return count;
        }

        public async Task<Dictionary<string, int>> GetCategoryCountsAsync()
        {
            return await _context.Products
                .Where(p => p.IsPublished)
                .GroupBy(p => p.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Category, x => x.Count);
        }

        public async Task<int> ImportProductsAsync(IEnumerable<Product> products)
        {
            var list = products.ToList();
            _context.Products.AddRange(list);
            await _context.SaveChangesAsync();
            return list.Count;
        }

        public async Task<List<Product>> GetAllRawAsync()
        {
            return await _context.Products.AsNoTracking().ToListAsync();
        }

        // Updates only Cost Price, Recommended Retail, and Margin — never Price/Stock/
        // Category/Published, so a pricing correction can never silently change what
        // customers see or buy.
        public async Task<Product?> UpdatePricingAsync(int id, decimal costPrice, decimal? recommendedRetail, decimal? margin)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return null;

            product.CostPrice = costPrice;
            product.RecommendedRetail = recommendedRetail;
            product.Margin = margin;
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<HashSet<string>> GetExistingCodesAsync(IEnumerable<string> codes)
        {
            var codeList = codes.ToList();
            return (await _context.Products
                .Where(p => p.Code != null && codeList.Contains(p.Code))
                .Select(p => p.Code!)
                .ToListAsync())
                .ToHashSet();
        }

        public async Task SetCodeAsync(int id, string code)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.Code = code;
                await _context.SaveChangesAsync();
            }
        }

        public async Task CreateInitialBatchAsync(int productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || quantity <= 0) return;

            _context.ProductBatches.Add(new ProductBatch
            {
                ProductId = productId,
                BatchNumber = "1",
                Quantity = quantity,
                RemainingQuantity = quantity,
            });
            product.StockQuantity = quantity;

            await _context.SaveChangesAsync();
        }
    }
}
