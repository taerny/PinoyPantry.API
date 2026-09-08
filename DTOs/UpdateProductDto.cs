namespace PinoyPantry.API.DTOs
{
    public class UpdateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        // Stock is intentionally not accepted here — it's managed entirely via batches (see
        // ProductBatchService), never edited directly through the product form.
        public bool IsPublished { get; set; }

        // Pure profit margin (fraction, e.g. 0.20 = 20%), GST-exclusive — drives the
        // server-computed RecommendedRetail. See PricingCalculator.
        public decimal? Margin { get; set; }

        // Code is intentionally not accepted here either — it's set once at creation (or by
        // the PDF importer) and locked from then on. It's the key future invoices are matched
        // against, so letting it drift after the fact would silently break that reconciliation.

        // CostPrice/PackQty/Subtotal are intentionally not accepted here — cost now lives per
        // batch (ProductBatch.CostPrice/Subtotal), never written directly on the product, so a
        // product edit can never silently overwrite the batch-synced cost. See
        // BatchStockService.SyncProductCostAsync.
    }
}
