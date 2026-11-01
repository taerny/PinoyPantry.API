namespace PinoyPantry.API.DTOs
{
    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int StockQuantity { get; set; }

        // Website copy written ahead of time (e.g. from an import), shown on the product page.
        public string ShortDescription { get; set; } = string.Empty;

        // When the product was first published - used for the "NEW" badge and New Arrivals.
        public DateTime? PublishedAt { get; set; }
    }
}
