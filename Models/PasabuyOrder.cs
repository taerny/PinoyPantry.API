namespace PinoyPantry.API.Models
{
    public class PasabuyOrder
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // Optional — some customers only give a phone number.
        public string? Email { get; set; }

        // Legacy free-text list of items — superseded by the structured Items below (each a
        // real name/qty/image/notes row), but kept around for old submissions that only ever
        // had this field. New orders leave it blank.
        public string? ItemsRequested { get; set; }
        public string? Notes { get; set; }

        public List<PasabuyOrderItem> Items { get; set; } = new();

        // Set by the admin once they've followed up with the customer.
        public bool Contacted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
