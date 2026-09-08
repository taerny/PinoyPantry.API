using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PinoyPantry.API.Services
{
    public record PdfInvoiceRow(string Code, string Name);

    // Extracts Code + Name pairs from a supplier invoice PDF — nothing else. Quantity and Unit
    // Price are deliberately never trusted from this document (unreliable — sometimes a box
    // total, sometimes per-item, no way to tell from the PDF alone); the admin fills those in
    // by hand afterward from a separate cost document, per the agreed workflow.
    //
    // Reconstructs rows from raw positioned text rather than relying on any table structure in
    // the PDF itself: words are clustered into visual lines by Y-position, then a line starting
    // with something matching the supplier's code pattern (e.g. "UFC-0001-018") begins a new
    // row; any line without a leading code is treated as a continuation of the previous row's
    // name (wrapped text, origin notes, etc.) and merged in. This is a best-effort extraction —
    // the caller is expected to show the result for admin review before committing anything.
    public static class PdfInvoiceParseService
    {
        // 2-5 letter prefix, then 2 more hyphen-separated numeric groups — matches this
        // supplier's "UFC-0001-018" style codes without being so loose it grabs random text.
        private static readonly Regex CodePattern = new(@"^[A-Z]{2,5}-\d{2,4}-\d{1,4}$", RegexOptions.Compiled);

        // Lines at/after any of these end the product table — everything past this point is
        // invoice totals/terms/bank details, not products.
        private static readonly string[] StopMarkers =
        {
            "Payment Terms", "Product Cost:", "Sub Total:", "Tax Invoice Total",
            "Ownership of the goods", "Bank Details",
        };

        // Lines that are page furniture (repeated header/footer on every page) rather than
        // product rows — skipped outright, never merged into a name.
        private static readonly string[] NoiseMarkers =
        {
            "Tax Invoice", "Collected by", "Checked by", "Delivered by",
            "TEL :", "GST #", "t/a", "Invoice Date", "Invoice No", "Customer PO No",
            "Customer:", "Ship To:", "Code           Item", "Code            Item",
        };

        public static List<PdfInvoiceRow> Parse(Stream pdfStream)
        {
            var lines = ExtractLines(pdfStream);
            var rows = new List<PdfInvoiceRow>();
            string? currentCode = null;
            var currentName = new List<string>();

            void FlushCurrent()
            {
                if (currentCode == null) return;
                var name = string.Join(" ", currentName).Trim();
                if (!string.IsNullOrWhiteSpace(name))
                    rows.Add(new PdfInvoiceRow(currentCode, name));
                currentCode = null;
                currentName.Clear();
            }

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0) continue;

                if (StopMarkers.Any(m => trimmed.Contains(m, StringComparison.OrdinalIgnoreCase)))
                {
                    FlushCurrent();
                    break;
                }

                if (NoiseMarkers.Any(m => trimmed.Contains(m, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var firstToken = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

                if (CodePattern.IsMatch(firstToken))
                {
                    FlushCurrent();
                    currentCode = firstToken;
                    var rest = CleanNameFragment(trimmed[firstToken.Length..]);
                    if (rest.Length > 0) currentName.Add(rest);
                }
                else if (currentCode != null)
                {
                    var cleaned = CleanNameFragment(trimmed);
                    if (cleaned.Length > 0)
                        currentName.Add(cleaned);
                }
            }
            FlushCurrent();

            return rows;
        }

        // Origin/packaging-only fragments that sometimes end up as their own extracted line —
        // not part of the product name, dropped outright rather than appended.
        private static readonly string[] PureNoiseFragments =
        {
            "Philipp.", "Philpp.", "Ind.", "(small box)", "Coming",
        };

        // A code-line and a continuation line both come out of PdfPig as "every word at this
        // Y-position, left to right" — which includes the UOM/Qty/Unit Price/Subtotal columns
        // on the same physical line as the name, not just the Item column. This truncates at
        // the first sign of those other columns (the UOM's leading "x", or a dollar amount) so
        // only the actual name text is kept.
        private static string CleanNameFragment(string text)
        {
            var trimmed = text.Trim();
            if (PureNoiseFragments.Any(n => trimmed.Equals(n, StringComparison.OrdinalIgnoreCase)))
                return "";

            // UOM column always starts with a standalone "x" followed by a count/unit, e.g.
            // "x 18units/carton", "x 1unit", "x 6sets/carton", "x 1 pallet".
            var uomMatch = Regex.Match(trimmed, @"\bx\s+\d");
            if (uomMatch.Success)
                trimmed = trimmed[..uomMatch.Index].Trim();

            // Fallback: truncate at the first dollar amount if UOM wasn't found on this line.
            var priceMatch = Regex.Match(trimmed, @"\$[\d,]+\.\d{2}");
            if (priceMatch.Success)
                trimmed = trimmed[..priceMatch.Index].Trim();

            return trimmed;
        }

        private static List<string> ExtractLines(Stream pdfStream)
        {
            using var document = PdfDocument.Open(pdfStream);
            var lines = new List<string>();

            foreach (var page in document.GetPages())
            {
                var words = page.GetWords().ToList();
                if (words.Count == 0) continue;

                // Cluster words into visual lines by Y-position — words within a few points of
                // each other vertically are on the same line, regardless of column.
                var sorted = words.OrderByDescending(w => w.BoundingBox.Bottom).ToList();
                var currentLineWords = new List<Word> { sorted[0] };
                var currentY = sorted[0].BoundingBox.Bottom;

                void FlushLine()
                {
                    if (currentLineWords.Count == 0) return;
                    var text = string.Join(" ", currentLineWords.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));
                    lines.Add(text);
                    currentLineWords.Clear();
                }

                for (var i = 1; i < sorted.Count; i++)
                {
                    var word = sorted[i];
                    if (Math.Abs(word.BoundingBox.Bottom - currentY) <= 3)
                    {
                        currentLineWords.Add(word);
                    }
                    else
                    {
                        FlushLine();
                        currentLineWords.Add(word);
                        currentY = word.BoundingBox.Bottom;
                    }
                }
                FlushLine();
            }

            return lines;
        }
    }
}
