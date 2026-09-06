namespace PinoyPantry.API.Models
{
    // Single-row settings table — editable bank transfer details shown on checkout,
    // invoices, and order emails.
    public class BankDetails
    {
        public int Id { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
