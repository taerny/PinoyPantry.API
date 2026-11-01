namespace PinoyPantry.API.DTOs
{
    public class ProductQueryParams
    {
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 12;
        public string? Category { get; set; }
        public string? Search { get; set; }
        public bool IncludeUnpublished { get; set; } = false;

        // "newest" = most recently published first.
        public string? Sort { get; set; }

        // Only products first published within the last NewDays days and currently in stock.
        public bool NewOnly { get; set; } = false;
    }
}
