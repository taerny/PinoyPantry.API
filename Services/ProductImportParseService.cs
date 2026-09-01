using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using PinoyPantry.API.DTOs;

namespace PinoyPantry.API.Services
{
    // Parses a supplier's raw pricing sheet (.xlsx or .csv) straight into import-ready rows —
    // no manual column renaming or format conversion required from the admin. Header names are
    // matched against common supplier-sheet spellings, and Category is guessed from keywords in
    // the product name when the sheet doesn't have one.
    public static class ProductImportParseService
    {
        // Each field accepts several supplier-sheet header spellings. Only used to identify which
        // raw column plays which role — the columns themselves are always shown as-is, in their
        // original order, never renamed or reordered.
        private static readonly string[] NameHeaders = { "name", "product", "productname", "title", "item" };
        private static readonly string[] CategoryHeaders = { "category", "cat" };
        private static readonly string[] StockHeaders = { "stockquantity", "quantity", "qty", "stock" };
        private static readonly string[] CostHeaders = { "costprice", "costexgst", "cost", "wholesalecost", "unitcost" };
        private static readonly string[] RecommendedRetailHeaders = { "recommendedretail", "rrp", "retailprice" };
        private static readonly string[] MarginHeaders = { "margin" };
        private static readonly string[] PublishedHeaders = { "ispublished", "published" };

        // [keyword, category] — first matching keyword wins, so order guards against overlaps
        // (e.g. "crispy fry" is checked as a mix before the generic "sauce" match could apply).
        private static readonly (string Keyword, string Category)[] CategoryKeywords =
        {
            ("dried", "Dried Fish"), ("danggit", "Dried Fish"), ("bisugo", "Dried Fish"), ("sap-sap", "Dried Fish"), ("tuyo", "Dried Fish"), ("tinapa", "Dried Fish"),
            ("pancit", "Noodles"), ("noodle", "Noodles"), ("vermicelli", "Noodles"), ("palabok", "Noodles"), ("sotanghon", "Noodles"), ("bihon", "Noodles"), ("instant bowl", "Noodles"), ("chillimansi", "Noodles"), ("canton", "Noodles"),
            ("sinigang", "Soups & Mixes"), ("broth", "Soups & Mixes"), ("kare-kare", "Soups & Mixes"), ("kare kare", "Soups & Mixes"), ("pinapaitan", "Soups & Mixes"), ("tocino mix", "Soups & Mixes"), ("crispy fry", "Soups & Mixes"), ("mix", "Soups & Mixes"), ("soup", "Soups & Mixes"),
            ("milk", "Dairy"), ("cream", "Dairy"), ("cheese", "Dairy"), ("cheezee", "Dairy"), ("evaporated", "Dairy"), ("yogurt", "Dairy"),
            ("sauce", "Condiments"), ("vinegar", "Condiments"), ("ketchup", "Condiments"), ("seasoning", "Condiments"), ("msg", "Condiments"), ("bagoong", "Condiments"), ("patis", "Condiments"), ("toyo", "Condiments"), ("annatto", "Condiments"), ("achuete", "Condiments"),
            ("fruit cocktail", "Canned Goods"), ("nata de coco", "Canned Goods"), ("kaong", "Canned Goods"), ("corned beef", "Canned Goods"), ("sardines", "Canned Goods"), ("spam", "Canned Goods"), ("luncheon meat", "Canned Goods"), ("coconut strings", "Canned Goods"), ("canned", "Canned Goods"),
            ("candy", "Sweets"), ("polvoron", "Sweets"), ("pastillas", "Sweets"), ("choco", "Sweets"), ("toffee", "Sweets"),
            ("chips", "Snacks"), ("cracker", "Snacks"), ("cookie", "Snacks"), ("biscuit", "Snacks"), ("otap", "Snacks"), ("biscocho", "Snacks"), ("nuts", "Snacks"), ("cornick", "Snacks"), ("bawang", "Snacks"), ("chicharon", "Snacks"),
            ("juice", "Beverages"), ("tea", "Beverages"), ("drink", "Beverages"), ("soda", "Beverages"), ("milo", "Beverages"),
            ("rice", "Rice & Grains"), ("malagkit", "Rice & Grains"), ("sinandomeng", "Rice & Grains"),
            ("frozen", "Frozen"), ("lumpia", "Frozen"), ("siomai", "Frozen"), ("ice cream", "Frozen"),
        };

        public static List<ImportPreviewRowDto> Parse(Stream fileStream, string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            var (headers, dataRows) = extension switch
            {
                ".xlsx" or ".xls" => ParseExcel(fileStream),
                ".csv" => ParseCsv(fileStream),
                _ => throw new InvalidOperationException($"Unsupported file type '{extension}'. Upload a .csv or .xlsx file."),
            };

            return BuildRows(headers, dataRows);
        }

        private static (List<string> Headers, List<List<string>> DataRows) ParseExcel(Stream fileStream)
        {
            using var workbook = new XLWorkbook(fileStream);

            // A pricing workbook often has extra sheets (Summary, Assumptions, legends, etc.) —
            // the actual product list is reliably the sheet with the most used rows, regardless
            // of what it's named.
            var sheet = workbook.Worksheets
                .OrderByDescending(ws => ws.LastRowUsed()?.RowNumber() ?? 0)
                .First();

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
            var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 1;

            var headers = new List<string>();
            for (var c = 1; c <= lastCol; c++)
                headers.Add(sheet.Cell(1, c).GetString().Trim());

            var dataRows = new List<List<string>>();
            for (var r = 2; r <= lastRow; r++)
            {
                var row = new List<string>();
                for (var c = 1; c <= lastCol; c++)
                    row.Add(sheet.Cell(r, c).GetString().Trim());
                dataRows.Add(row);
            }

            return (headers, dataRows);
        }

        private static (List<string> Headers, List<List<string>> DataRows) ParseCsv(Stream fileStream)
        {
            using var reader = new StreamReader(fileStream);
            var text = reader.ReadToEnd();
            var lines = text.Trim().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            if (lines.Length < 2) return (new List<string>(), new List<List<string>>());

            var headers = ParseCsvLine(lines[0]);
            var dataRows = lines.Skip(1)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(ParseCsvLine)
                .ToList();

            return (headers, dataRows);
        }

        // Splits a CSV line respecting quoted fields (handles commas inside quotes)
        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = "";
            var inQuotes = false;
            foreach (var ch in line)
            {
                if (ch == '"') inQuotes = !inQuotes;
                else if (ch == ',' && !inQuotes) { result.Add(current.Trim()); current = ""; }
                else current += ch;
            }
            result.Add(current.Trim());
            return result;
        }

        // Strips spaces/punctuation so header variants like "Cost ex-GST" and "costexgst" match
        private static string NormalizeHeader(string h) => Regex.Replace(h.ToLowerInvariant(), "[^a-z0-9]", "");

        private static int FindColumn(List<string> normalizedHeaders, string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                var idx = normalizedHeaders.IndexOf(candidate);
                if (idx != -1) return idx;
            }
            return -1;
        }

        private static string Escape(string s) => Regex.Escape(s);

        // Rounds numeric-looking reference columns (GST, Margin, Total Revenue, etc.) to 2
        // decimal places for readability — Excel's raw doubles otherwise show as long strings
        // like "3.739130434782609". Non-numeric values pass through unchanged.
        private static string FormatExtraValue(string value) =>
            decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num)
                ? Math.Round(num, 2).ToString(CultureInfo.InvariantCulture)
                : value;

        // Whole-word match (with an optional trailing "s" for plurals like "Crackers"/"Sauces")
        // so e.g. the "cream" keyword doesn't fire on "creamy" inside an unrelated product name.
        private static bool MatchesKeyword(string name, string keyword) =>
            Regex.IsMatch(name, $@"\b{Escape(keyword)}s?\b", RegexOptions.IgnoreCase);

        // Guesses a category from keywords in the product name. Only ever called when the source
        // sheet left Category blank — an explicit value from the file always wins over a guess.
        private static string GuessCategory(string name)
        {
            foreach (var (keyword, category) in CategoryKeywords)
            {
                if (MatchesKeyword(name, keyword)) return category;
            }
            return "";
        }

        private static List<ImportPreviewRowDto> BuildRows(List<string> headers, List<List<string>> dataRows)
        {
            var normalizedHeaders = headers.Select(NormalizeHeader).ToList();
            var nameIdx = FindColumn(normalizedHeaders, NameHeaders);
            var categoryIdx = FindColumn(normalizedHeaders, CategoryHeaders);
            var stockIdx = FindColumn(normalizedHeaders, StockHeaders);
            var costIdx = FindColumn(normalizedHeaders, CostHeaders);
            var recommendedRetailIdx = FindColumn(normalizedHeaders, RecommendedRetailHeaders);
            var marginIdx = FindColumn(normalizedHeaders, MarginHeaders);
            var publishedIdx = FindColumn(normalizedHeaders, PublishedHeaders);

            string RoleFor(int idx) =>
                idx == nameIdx ? "name" :
                idx == stockIdx ? "stock" :
                idx == costIdx ? "cost" :
                idx == recommendedRetailIdx ? "recommendedRetail" :
                idx == marginIdx ? "margin" :
                "reference";

            var rows = new List<ImportPreviewRowDto>();
            foreach (var cols in dataRows)
            {
                string Get(int idx) => idx != -1 && idx < cols.Count ? cols[idx] : "";

                var name = Get(nameIdx);
                if (string.IsNullOrWhiteSpace(name)) continue;

                // An explicit Category column (if the sheet has one) always wins over a guess.
                var explicitCategory = categoryIdx != -1 ? Get(categoryIdx).Trim() : "";
                var category = string.IsNullOrEmpty(explicitCategory) ? GuessCategory(name) : explicitCategory;
                var isPublished = publishedIdx != -1 && Get(publishedIdx).Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

                // Every column from the file, in its original order and with its original header
                // text — nothing hidden, renamed, or reordered. Columns that also play a role
                // (name/stock/cost/recommendedRetail/margin) are editable and feed the import
                // payload directly from this same list; everything else is read-only reference.
                var columns = new List<ImportColumnDto>();
                for (var i = 0; i < headers.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(headers[i])) continue;
                    columns.Add(new ImportColumnDto
                    {
                        Name = headers[i],
                        Value = FormatExtraValue(Get(i)),
                        Role = RoleFor(i),
                    });
                }

                rows.Add(new ImportPreviewRowDto
                {
                    Columns = columns,
                    Category = category,
                    CategoryGuessed = string.IsNullOrEmpty(explicitCategory) && !string.IsNullOrEmpty(category),
                    Price = 0, // always starts at 0 — the admin's own store price, never auto-filled
                    IsPublished = isPublished,
                });
            }

            return rows;
        }
    }
}
