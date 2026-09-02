using System.ComponentModel.DataAnnotations.Schema;

namespace PinoyPantry.API.Models
{
    public class Order
    {
        public int Id { get; set; }

        // Set right after insert, once Id is known — see OrderService.CreateOrderAsync.
        // Nullable only for the instant between insert and that follow-up update.
        public string? InvoiceNumber { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public string? Notes { get; set; }

        public string? DeliveryMethod { get; set; }

        // Null means "not yet known" (Delivery outside Dunedin, fee to be arranged) — distinct
        // from 0.00, which means "confirmed, no charge" (Click & Collect).
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DeliveryFee { get; set; }

        // Pending -> Paid -> Completed, or Pending -> Cancelled. No transitions out of
        // Cancelled/Completed — enforced in OrderService, not at the DB level.
        public string Status { get; set; } = "Pending";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<OrderItem> Items { get; set; } = new();
    }
}
