using Microsoft.EntityFrameworkCore;
using PinoyPantry.API.Data;
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

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Product?> GetProductyByIdAsync(int id)
        {
            return  await _context.Products.FindAsync(id);
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
            var existingProduct =  await _context.Products.FindAsync(id);
            if (existingProduct == null)
                return null;    
            existingProduct.Name = product.Name;    
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.ImageUrll = product.ImageUrll;  
            existingProduct.Category = product.Category;    
            existingProduct.StockQuantity = product.StockQuantity;

            await _context.SaveChangesAsync(); 
            return existingProduct;
        }
    }
}
