using Microsoft.EntityFrameworkCore;
using PinoyPantry.API.Data;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Services
{
    public class ProductBatchService : IProductBatchService
    {
        private readonly ApplicationDBContext _context;
        private readonly IBatchStockService _batchStockService;

        public ProductBatchService(ApplicationDBContext context, IBatchStockService batchStockService)
        {
            _context = context;
            _batchStockService = batchStockService;
        }

        public async Task<ProductBatchListResponseDto?> GetBatchesAsync(int productId)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
            if (!productExists) return null;

            var batches = await _context.ProductBatches
                .Where(b => b.ProductId == productId)
                .OrderBy(b => b.CreatedAt).ThenBy(b => b.Id)
                .ToListAsync();

            // Fill the lowest free number rather than always continuing from the highest —
            // batch numbers mirror what's physically written on the product, so deleting
            // batch 2 should offer "2" again next time, not permanently skip it in favour of 4.
            var usedNumbers = batches
                .Select(b => b.BatchNumber)
                .Where(n => n.All(char.IsDigit) && n.Length > 0)
                .Select(int.Parse)
                .ToHashSet();

            var nextNumber = 1;
            while (usedNumbers.Contains(nextNumber)) nextNumber++;

            return new ProductBatchListResponseDto
            {
                Batches = batches.Select(ToDto).ToList(),
                SuggestedNextBatchNumber = nextNumber.ToString(),
            };
        }

        public async Task<(ProductBatchDto? Batch, string? Error)> CreateBatchAsync(int productId, CreateProductBatchDto dto)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return (null, null);

            if (string.IsNullOrWhiteSpace(dto.BatchNumber))
                return (null, "Batch number is required.");

            if (dto.Quantity < 1)
                return (null, "Quantity must be at least 1.");

            if (dto.CostPrice < 0)
                return (null, "Cost price cannot be negative.");

            var duplicate = await _context.ProductBatches
                .AnyAsync(b => b.ProductId == productId && b.BatchNumber == dto.BatchNumber);
            if (duplicate)
                return (null, $"Batch \"{dto.BatchNumber}\" already exists for this product.");

            var batch = new ProductBatch
            {
                ProductId = productId,
                BatchNumber = dto.BatchNumber,
                Quantity = dto.Quantity,
                RemainingQuantity = dto.Quantity,
                CostPrice = dto.CostPrice,
                Subtotal = dto.CostPrice * dto.Quantity,
                BestBefore = dto.BestBefore,
            };

            _context.ProductBatches.Add(batch);
            product.StockQuantity += dto.Quantity;
            await _context.SaveChangesAsync(); // batch needs an Id before it can be "the active one"

            await _batchStockService.SyncProductCostAsync(product);
            await _context.SaveChangesAsync();

            return (ToDto(batch), null);
        }

        public async Task<bool?> DeleteBatchAsync(int productId, int batchId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return null;

            var batch = await _context.ProductBatches
                .FirstOrDefaultAsync(b => b.Id == batchId && b.ProductId == productId);
            if (batch == null) return false;

            // Only the portion still remaining (unsold) comes back off the product's total —
            // units already sold from this batch stay sold.
            product.StockQuantity = Math.Max(0, product.StockQuantity - batch.RemainingQuantity);
            _context.ProductBatches.Remove(batch);
            await _context.SaveChangesAsync();

            await _batchStockService.SyncProductCostAsync(product);
            await _context.SaveChangesAsync();

            return true;
        }

        private static ProductBatchDto ToDto(ProductBatch batch) => new()
        {
            Id = batch.Id,
            BatchNumber = batch.BatchNumber,
            Quantity = batch.Quantity,
            RemainingQuantity = batch.RemainingQuantity,
            CostPrice = batch.CostPrice,
            Subtotal = batch.Subtotal,
            BestBefore = batch.BestBefore,
            CreatedAt = batch.CreatedAt,
        };
    }
}
