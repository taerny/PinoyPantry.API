namespace PinoyPantry.API.DTOs
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Data { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Limit);
    }
}
