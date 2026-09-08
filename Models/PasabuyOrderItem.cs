namespace PinoyPantry.API.Models
{
    // One requested product within a Pasabuy order. Pasabuy items aren't in the regular
    // catalog (that's the whole point — customer is asking us to source something from the
    // Philippines), so this is its own free-form line item, not a link to Product.
    public class PasabuyOrderItem
    {
        public int Id { get; set; }

        public int PasabuyOrderId { get; set; }
        public PasabuyOrder? PasabuyOrder { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;

        // Optional reference photo (e.g. a screenshot from Facebook/Shopee) so we can match
        // the exact brand/packaging when buying — uploaded via the public Pasabuy image
        // endpoint, same storage as product images.
        public string? ImageUrl { get; set; }

        // Variant/brand preference, e.g. "spicy version", "family size".
        public string? Notes { get; set; }
    }
}
