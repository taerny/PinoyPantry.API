namespace PinoyPantry.API.DTOs
{
    public class ProductQueryParams
    {
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 12;
        public string? Category { get; set; }
        public string? Search { get; set; }
    }
}
