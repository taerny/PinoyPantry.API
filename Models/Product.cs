namespace PinoyPantry.API.Models
{
    public class Product
    {

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string ImageUrll { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public int StockQuantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    }
}
