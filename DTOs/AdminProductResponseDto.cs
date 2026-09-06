using System.Text.Json.Serialization;

namespace PinoyPantry.API.DTOs
{
    public class AdminProductResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public bool IsPublished { get; set; }

        // Server-computed — see PricingCalculator. Never set directly.
        public decimal? RecommendedRetail { get; set; }
        public decimal? Margin { get; set; }

        public string? Code { get; set; }

        [JsonPropertyName("qty")]
        public int? PackQty { get; set; }

        public decimal? Subtotal { get; set; }

        // Breakdown of the ACTUAL store Price (not RecommendedRetail) — reflects any manual
        // rounding the admin has applied.
        public decimal ProfitAmount { get; set; }
        public decimal GstAmount { get; set; }
        public decimal GstRate { get; set; }
    }
}
