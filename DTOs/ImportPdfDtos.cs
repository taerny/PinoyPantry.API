namespace PinoyPantry.API.DTOs
{
    public class ImportPdfPreviewRowDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        // True if a product with this Code already exists — shown in the review list so the
        // admin can skip it rather than create a duplicate. The confirm step also enforces
        // this server-side regardless of what the client sends.
        public bool AlreadyExists { get; set; }
    }

    // Category isn't asked for here — the PDF doesn't have it either, and it's fine to fill in
    // later via the normal Edit form (same as Cost/Quantity/Batch), same reasoning as why these
    // drafts are created unpublished.
    public class ConfirmImportPdfRowDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
