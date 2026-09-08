using System.ComponentModel.DataAnnotations.Schema;

namespace PinoyPantry.API.Models
{
    // A physical batch/restock of a product — e.g. "Batch 2" of UFC Banana Sauce. Stock is
    // drawn from the oldest batch first (FIFO, see BatchStockService); Product.StockQuantity
    // stays the single source of truth, batches are a best-effort breakdown of it for the
    // admin's own visibility into freshness/expiry.
    //
    // Cost lives here, not on Product — different batches of the same product genuinely cost
    // different amounts (supplier prices change between shipments). Product.CostPrice is kept
    // in sync with whichever batch is currently the active FIFO-selling one (see
    // BatchStockService.SyncProductCostAsync), so pricing always reflects what's actually
    // being sold right now, not a blend across old and new stock.
    public class ProductBatch
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public string BatchNumber { get; set; } = string.Empty;

        public int Quantity { get; set; }
        public int RemainingQuantity { get; set; }

        // Per-unit cost for this specific batch, entered by the admin from the supplier's
        // invoice/other cost document — not derived from anything on the PDF import.
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        // CostPrice * Quantity — this batch's total cost, computed once at creation time.
        // Read-only record of what this shipment actually cost, never independently edited.
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        public DateTime? BestBefore { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
