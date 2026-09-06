using System.ComponentModel.DataAnnotations.Schema;

namespace PinoyPantry.API.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        public bool IsPublished { get; set; } = false;

        // RecommendedRetail is derived — always recomputed from CostPrice/Margin/GST (see
        // PricingCalculator) whenever either input changes. Never set directly by an admin.
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RecommendedRetail { get; set; }

        // Pure profit margin as a fraction (0.20 = 20%), GST-exclusive — admin-set per product,
        // drives the RecommendedRetail formula. Not the same as markup.
        [Column(TypeName = "decimal(18,4)")]
        public decimal? Margin { get; set; }

        // Supplier invoice reference data (Code/Subtotal) plus the pack size needed to derive
        // CostPrice from it — see PricingCalculator.UnitCost. Subtotal is locked once set: it's
        // the source-of-truth invoice line amount, never edited through the UI.
        public string? Code { get; set; }

        public int? PackQty { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Subtotal { get; set; }
    }
}
