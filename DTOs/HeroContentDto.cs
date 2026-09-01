namespace PinoyPantry.API.DTOs
{
    public class HeroContentDto
    {
        public string Headline { get; set; } = string.Empty;
        public string HighlightedText { get; set; } = string.Empty;
        public string Subtext { get; set; } = string.Empty;
        public string ButtonText { get; set; } = string.Empty;
        public string ButtonLink { get; set; } = string.Empty;
        public string FooterAboutText { get; set; } = string.Empty;
        public string TopBarText { get; set; } = string.Empty;
        public bool IsMaintenanceMode { get; set; } = false;
        public string MaintenanceHeadline { get; set; } = string.Empty;
        public string MaintenanceMessage { get; set; } = string.Empty;
    }
}
