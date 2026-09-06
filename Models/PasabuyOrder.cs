namespace PinoyPantry.API.Models
{
    public class PasabuyOrder
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // Optional — some customers only give a phone number.
        public string? Email { get; set; }

        // Free-text list of items wanted — these aren't in the regular product
        // catalog, so a rigid line-item picker doesn't fit this use case.
        public string ItemsRequested { get; set; } = string.Empty;
        public string? Notes { get; set; }

        // Set by the admin once they've followed up with the customer.
        public bool Contacted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
