namespace PinoyPantry.API.DTOs
{
    public class ProductBatchDto
    {
        public int Id { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int RemainingQuantity { get; set; }
        public DateTime? BestBefore { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductBatchListResponseDto
    {
        public List<ProductBatchDto> Batches { get; set; } = new();
        public string SuggestedNextBatchNumber { get; set; } = "1";
    }

    public class CreateProductBatchDto
    {
        public string BatchNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime? BestBefore { get; set; }
    }
}
