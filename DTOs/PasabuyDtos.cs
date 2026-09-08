namespace PinoyPantry.API.DTOs
{
    public class CreatePasabuyOrderItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public string? ImageUrl { get; set; }
        public string? Notes { get; set; }
    }

    public class CreatePasabuyOrderDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public List<CreatePasabuyOrderItemDto> Items { get; set; } = new();
        public string? Notes { get; set; }
    }

    public class PasabuyOrderItemResponseDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? Notes { get; set; }
    }

    public class PasabuyOrderResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public List<PasabuyOrderItemResponseDto> Items { get; set; } = new();

        // Kept for old orders submitted before per-item entry existed.
        public string? ItemsRequested { get; set; }
        public string? Notes { get; set; }
        public bool Contacted { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SetPasabuyContactedDto
    {
        public bool Contacted { get; set; }
    }
}
