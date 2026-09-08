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

            // Fetch every batch (not just ones with stock left) — the active-cost check below
            // needs the full, freshly-mutated picture; a filtered re-query after mutating would
            // still see the old, unsaved RemainingQuantity values from the database.
            var batches = await _context.ProductBatches
                .Where(b => b.ProductId == product.Id)
                .OrderBy(b => b.CreatedAt).ThenBy(b => b.Id)
                .ToListAsync();

            foreach (var batch in batches)
            {
                if (remaining <= 0) break;
                if (batch.RemainingQuantity <= 0) continue;

                var take = Math.Min(batch.RemainingQuantity, remaining);
                batch.RemainingQuantity -= take;
                remaining -= take;
            }

            // Stock always moves 1:1 with the order regardless of batch state (e.g. a product
            // with no batches yet) — batches are a best-effort breakdown of that total, not
            // the source of truth for it.
            product.StockQuantity -= quantity;
            SyncProductCostFromBatches(product, batches);
        }

        public async Task RestockAsync(Product product, int quantity)
        {
            var remaining = quantity;

            var batches = await _context.ProductBatches
                .Where(b => b.ProductId == product.Id)
                .OrderBy(b => b.CreatedAt).ThenBy(b => b.Id)
                .ToListAsync();

            foreach (var batch in batches)
            {
                if (remaining <= 0) break;

                var room = batch.Quantity - batch.RemainingQuantity;
                if (room <= 0) continue;

                var give = Math.Min(room, remaining);
                batch.RemainingQuantity += give;
                remaining -= give;
            }

            product.StockQuantity += quantity;
            SyncProductCostFromBatches(product, batches);
        }

        public async Task SyncProductCostAsync(Product product)
        {
            // Safe to query fresh here — callers (ProductBatchService) save their batch
            // changes before calling this, unlike Deduct/RestockAsync above which sync from
            // their own in-memory list precisely to avoid reading stale unsaved state.
            var activeBatch = await _context.ProductBatches
                .Where(b => b.ProductId == product.Id && b.RemainingQuantity > 0)
                .OrderBy(b => b.CreatedAt).ThenBy(b => b.Id)
                .FirstOrDefaultAsync();

            ApplyActiveBatchCost(product, activeBatch);
        }

        private static void SyncProductCostFromBatches(Product product, List<ProductBatch> batches)
        {
            var activeBatch = batches.FirstOrDefault(b => b.RemainingQuantity > 0);
            ApplyActiveBatchCost(product, activeBatch);
        }

        private static void ApplyActiveBatchCost(Product product, ProductBatch? activeBatch)
        {
            // No batch currently has stock — leave the last-known cost as-is rather than
            // resetting to 0 just because the shelf is momentarily empty.
            if (activeBatch == null) return;

            var costChanged = product.CostPrice != activeBatch.CostPrice;
            product.CostPrice = activeBatch.CostPrice;
            product.RecommendedRetail = PricingCalculator.RecommendedPrice(product.CostPrice, product.Margin);

            // Store Price follows Recommended Retail automatically, but only when the active
            // batch's cost genuinely changed (a real FIFO switch, or the product's first-ever
            // batch) — never on an ordinary sale against the same still-active batch, which
            // would otherwise silently undo any manual rounding (e.g. $2.99) on every order.
            // This is what makes pricing stay correct even if no admin is around when a batch
            // sells out; it also doubles as sensible default pricing the first time a brand-new
            // product gets its first batch.
            if (costChanged && product.RecommendedRetail.HasValue)
                product.Price = product.RecommendedRetail.Value;
        }
    }
}
