namespace PinoyPantry.API.DTOs
{
    public class CreatePasabuyOrderDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string ItemsRequested { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class PasabuyOrderResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string ItemsRequested { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool Contacted { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SetPasabuyContactedDto
    {
        public bool Contacted { get; set; }
    }
}
