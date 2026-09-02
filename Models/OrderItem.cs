using System.ComponentModel.DataAnnotations.Schema;

namespace PinoyPantry.API.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        // Nullable so a later product deletion doesn't break historical order records —
        // the name/price snapshot below is what actually matters for the order itself.
        public int? ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Quantity { get; set; }
    }
}
