using Microsoft.EntityFrameworkCore;
using PinoyPantry.API.Data;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Services
{
    // Keeps a product's batches (and its own StockQuantity) in sync as orders are placed and
    // cancelled. Stock is always drawn from the oldest batch first (FIFO) — a batch is never
    // touched until the one before it is fully depleted, and restocking on cancellation
    // refills batches in that same oldest-first order. Callers are expected to already hold
    // whatever lock/transaction the caller's own flow uses, same as OrderService today.
    public class BatchStockService : IBatchStockService
    {
        private readonly ApplicationDBContext _context;

        public BatchStockService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task DeductAsync(Product product, int quantity)
        {
            var remaining = quantity;

            var batches = await _context.ProductBatches
                .Where(b => b.ProductId == product.Id && b.RemainingQuantity > 0)
                .OrderBy(b => b.CreatedAt).ThenBy(b => b.Id)
                .ToListAsync();

            foreach (var batch in batches)
            {
                if (remaining <= 0) break;

                var take = Math.Min(batch.RemainingQuantity, remaining);
                batch.RemainingQuantity -= take;
                remaining -= take;
            }

            // Stock always moves 1:1 with the order regardless of batch state (e.g. a product
            // with no batches yet) — batches are a best-effort breakdown of that total, not
            // the source of truth for it.
            product.StockQuantity -= quantity;
        }

        public async Task RestockAsync(Product product, int quantity)
        {
            var remaining = quantity;

            var batches = await _context.ProductBatches
                .Where(b => b.ProductId == product.Id && b.RemainingQuantity < b.Quantity)
                .OrderBy(b => b.CreatedAt).ThenBy(b => b.Id)
                .ToListAsync();

            foreach (var batch in batches)
            {
                if (remaining <= 0) break;

                var room = batch.Quantity - batch.RemainingQuantity;
                var give = Math.Min(room, remaining);
                batch.RemainingQuantity += give;
                remaining -= give;
            }

            product.StockQuantity += quantity;
        }
    }
}
