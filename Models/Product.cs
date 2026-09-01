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

        // Reference figures carried over from supplier pricing sheets — optional, not shown
        // to customers. Margin is stored as a fraction (0.27 = 27%), matching the source data.
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RecommendedRetail { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? Margin { get; set; }
    }
}
