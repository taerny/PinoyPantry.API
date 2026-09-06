using System.Text.Json.Serialization;

namespace PinoyPantry.API.DTOs
{
    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public bool IsPublished { get; set; } = false;

        // Pure profit margin (fraction, e.g. 0.20 = 20%), GST-exclusive — drives the
        // server-computed RecommendedRetail. See PricingCalculator.
        public decimal? Margin { get; set; }

        // Supplier invoice reference data. CostPrice above is used as-is UNLESS both of these
        // are provided, in which case CostPrice is derived as Subtotal / Qty instead.
        public string? Code { get; set; }

        [JsonPropertyName("qty")]
        public int? PackQty { get; set; }

        public decimal? Subtotal { get; set; }
    }
}
