using System.ComponentModel.DataAnnotations.Schema;

namespace PinoyPantry.API.Models
{
    // A physical batch/restock of a product — e.g. "Batch 2" of UFC Banana Sauce. Stock is
    // drawn from the oldest batch first (FIFO, see BatchStockService); Product.StockQuantity
    // stays the single source of truth, batches are a best-effort breakdown of it for the
    // admin's own visibility into freshness/expiry.
    public class ProductBatch
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public string BatchNumber { get; set; } = string.Empty;

        public int Quantity { get; set; }
        public int RemainingQuantity { get; set; }

        public DateTime? BestBefore { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
