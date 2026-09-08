namespace PinoyPantry.API.DTOs
{
    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        // Stock is intentionally not accepted here — it starts at 0 and is only ever set by
        // adding a batch afterward (see ProductBatchService), matching Sugbodelights' approach.
        public bool IsPublished { get; set; } = false;

        // Pure profit margin (fraction, e.g. 0.20 = 20%), GST-exclusive — drives the
        // server-computed RecommendedRetail. See PricingCalculator.
        public decimal? Margin { get; set; }

        // Supplier's product code — optional, useful for matching future invoices/re-imports
        // to this same product. Leave blank and one auto-generates (see ProductService).
        public string? Code { get; set; }

        // CostPrice/PackQty/Subtotal are intentionally not accepted here — cost now lives per
        // batch (ProductBatch.CostPrice/Subtotal), never written directly on the product, so a
        // product edit can never silently overwrite the batch-synced cost. See
        // BatchStockService.SyncProductCostAsync.
    }
}
