namespace PinoyPantry.API.DTOs
{
    public class ImportPreviewRowDto
    {
        // Every column from the source file, in its original order, with its original header
        // text and value — nothing renamed, reordered, or hidden. Columns tagged with a Role
        // other than "reference" also double as the fields actually sent on import.
        public List<ImportColumnDto> Columns { get; set; } = new();

        // Synthesized, not read from any file column — most supplier sheets don't have a
        // Category column, so this is guessed from keywords in the product name and shown as
        // an editable dropdown in the review table.
        public string Category { get; set; } = string.Empty;
        public bool CategoryGuessed { get; set; }

        // Synthesized, not read from any file column — always starts at 0 so the admin sets
        // their own store price rather than it being silently filled from a promo/RRP-style
        // column in the sheet.
        public decimal Price { get; set; }

        public bool IsPublished { get; set; }
    }

    public class ImportColumnDto
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        // "name" | "stock" | "cost" | "recommendedRetail" | "margin" | "reference"
        // Non-reference roles are editable in the review table and feed the import payload;
        // "reference" columns (GST, Total Revenue, etc.) are shown read-only for context only.
        public string Role { get; set; } = "reference";
    }
}
