namespace PinoyPantry.API.DTOs
{
    public class PasabuySettingsDto
    {
        public string BadgeText { get; set; } = string.Empty;
        public string ClosedBadgeText { get; set; } = string.Empty;
        public string Heading { get; set; } = string.Empty;
        public string Intro { get; set; } = string.Empty;

        // yyyy-MM-dd, last day orders are accepted (inclusive, NZ time)
        public string CutoffDate { get; set; } = string.Empty;
        public string EstimatedArrival { get; set; } = string.Empty;
        public string Collection { get; set; } = string.Empty;

        public string FormTitle { get; set; } = string.Empty;
        public string FormBlurb { get; set; } = string.Empty;

        public List<string> HowItWorks { get; set; } = new();
        public string PricingText { get; set; } = string.Empty;
        public List<string> PleaseNote { get; set; } = new();

        public string ClosedMessage { get; set; } = string.Empty;
        public bool ForceClosed { get; set; }

        // Read-only, computed by the server (ignored on PUT).
        public bool IsOpen { get; set; }
        public string CutoffDisplay { get; set; } = string.Empty;
    }
}
