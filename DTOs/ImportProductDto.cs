namespace PinoyPantry.API.DTOs
{
    public class ImportProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal Price { get; set; }
        public bool IsPublished { get; set; }
        public decimal? RecommendedRetail { get; set; }
        public decimal? Margin { get; set; }
    }
}
