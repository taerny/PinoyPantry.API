namespace PinoyPantry.API.Models
{
    // Single-row settings table - everything editable about the homepage Pasabuy section, so the
    // admin can change the cut-off date, wording and rules for each shipment without a deploy.
    // Multi-line lists (HowItWorks / PleaseNote) are stored newline-separated, one entry per line.
    // Text may contain the {cutoff} placeholder, replaced with the formatted CutoffDate when shown.
    public class PasabuySettings
    {
        public int Id { get; set; }

        public string BadgeText { get; set; } = string.Empty;
        public string ClosedBadgeText { get; set; } = string.Empty;
        public string Heading { get; set; } = string.Empty;
        public string Intro { get; set; } = string.Empty;

        // Last day orders are accepted (inclusive, New Zealand time). Orders close automatically
        // once this date has passed.
        public DateTime CutoffDate { get; set; }
        public string EstimatedArrival { get; set; } = string.Empty;
        public string Collection { get; set; } = string.Empty;

        public string FormTitle { get; set; } = string.Empty;
        public string FormBlurb { get; set; } = string.Empty;

        public string HowItWorks { get; set; } = string.Empty;
        public string PricingText { get; set; } = string.Empty;
        public string PleaseNote { get; set; } = string.Empty;

        public string ClosedMessage { get; set; } = string.Empty;

        // Manual "close orders now" switch, e.g. when the shipment is already full.
        public bool ForceClosed { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
