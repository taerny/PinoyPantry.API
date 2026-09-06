namespace PinoyPantry.API.Services
{
    // Single source of truth for the pricing formula and the GST rate constant — see
    // ProductsController/AdminProductsPage for how these feed the product pricing fields.
    //
    // Margin and GST here are both applied as a fraction OF THE OUTPUT price (division), not
    // the input (multiplication) — this is intentional per spec, not standard NZ GST-inclusive
    // math (which would multiply by 1.15). Verified against the spec's worked example:
    //   unitCost 5.00, margin 20%, gst 15% -> priceBeforeGst 6.25 -> recommendedPrice 7.35
    public static class PricingCalculator
    {
        public const decimal GstRate = 0.15m;

        // unitCost = subtotal / packQty — the supplier invoice's line subtotal divided by how
        // many individual units it covers. Null if either input is missing/zero (can't divide).
        public static decimal? UnitCost(decimal? subtotal, int? packQty)
        {
            if (subtotal is null || packQty is null || packQty <= 0)
                return null;

            return subtotal.Value / packQty.Value;
        }

        // recommendedPrice, built up from unitCost + pure profit margin, then GST added the
        // same way (both as a fraction of the resulting price). Null if unitCost/margin are
        // missing, or margin/GST is 100%+ (division by zero or negative).
        public static decimal? RecommendedPrice(decimal? unitCost, decimal? profitMarginPct)
        {
            if (unitCost is null || profitMarginPct is null || profitMarginPct >= 1)
                return null;

            var priceBeforeGst = unitCost.Value / (1 - profitMarginPct.Value);
            return priceBeforeGst / (1 - GstRate);
        }

        // Breakdown computed from the ACTUAL store price (which may have been manually rounded
        // away from recommendedPrice), not from recommendedPrice — so the admin sees the real
        // profit/GST split on what's actually being charged.
        public static (decimal ProfitAmount, decimal GstAmount) Breakdown(decimal storePrice, decimal? unitCost)
        {
            var priceBeforeGst = storePrice * (1 - GstRate);
            var gstAmount = storePrice - priceBeforeGst;
            var profitAmount = priceBeforeGst - (unitCost ?? 0);
            return (profitAmount, gstAmount);
        }
    }
}
