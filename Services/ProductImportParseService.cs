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
        private static readonly string[] CostHeaders = { "costprice", "costexgst", "cost", "wholesalecost", "unitcost", "costindividual" };
        private static readonly string[] RecommendedRetailHeaders = { "recommendedretail", "rrp", "retailprice", "suggestedretail" };
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

        // Matches the "restock" sheet layout: Unit of Measure ("Case of 48"), Subtotal (Excl. GST),
        // Retail price with GST, Short Website Description. See BuildRows.
        private static readonly string[] UomHeaders = { "unitofmeasure", "uom" };
        private static readonly string[] PiecesPerCaseHeaders = { "piecespercase", "piecespercarton" };
        private static readonly string[] SubtotalExGstHeaders = { "subtotalexclgst", "subtotalexgst" };
        private static readonly string[] StorePriceHeaders = { "retailpricewithgst15", "retailpricewithgst", "storeprice", "sellingprice" };
        private static readonly string[] DescriptionHeaders = { "shortwebsitedescription", "shortdescription", "websitedescription" };

        private const decimal DefaultMargin = 0.20m;

        // Canonical names for the categories the store already uses, plus common sheet spellings.
        // Anything not listed falls through as Title Case and shows up as a "(new)" category in the
        // review table, so a brand-new category is always an explicit, visible choice.
        private static readonly Dictionary<string, string> CategoryAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["canned goods"] = "Canned Goods", ["canned good"] = "Canned Goods", ["canned"] = "Canned Goods",
            ["condiments"] = "Condiments", ["condiments and sauces"] = "Condiments", ["condiments & sauces"] = "Condiments", ["sauces"] = "Condiments",
            ["beverage"] = "Beverages", ["beverages"] = "Beverages", ["drinks"] = "Beverages",
            ["dairy"] = "Dairy", ["snacks"] = "Snacks", ["snack"] = "Snacks", ["noodles"] = "Noodles",
            ["soups & mixes"] = "Soups & Mixes", ["soups and mixes"] = "Soups & Mixes", ["dried fish"] = "Dried Fish",
            ["frozen"] = "Frozen", ["rice & grains"] = "Rice & Grains", ["rice and grains"] = "Rice & Grains", ["sweets"] = "Sweets",
        };

        private static string NormalizeCategory(string raw)
        {
            var key = Regex.Replace(raw.Trim(), @"\s+", " ");
            if (key.Length == 0) return "";
            if (CategoryAliases.TryGetValue(key, out var canonical)) return canonical;
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(key.ToLowerInvariant());
        }

        // Drops promo/placement tags that sit inside the name, e.g. "Juice 220ml (October promo)" or
        // "Coffee 300g (hanger)" - customers should not see them on the website.
        private static string CleanName(string name) =>
            Regex.Replace(name, @"\s*\((?:[^)]*\bpromo\b[^)]*|hanger)\)", "", RegexOptions.IgnoreCase).Trim();

        private static bool TryParseDecimal(string s, out decimal value) =>
            decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);

        // "Case of 48" -> 48. Falls back to the Pieces per Case column when the text has no number.
        private static int ParsePiecesPerCase(string uom, string piecesCol)
        {
            var m = Regex.Match(uom ?? "", @"(\d+)\s*$");
            if (m.Success && int.TryParse(m.Groups[1].Value, out var n) && n > 0) return n;
            return int.TryParse(piecesCol, out var p) && p > 0 ? p : 0;
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

            var uomIdx = FindColumn(normalizedHeaders, UomHeaders);
            var piecesIdx = FindColumn(normalizedHeaders, PiecesPerCaseHeaders);
            var subtotalExIdx = FindColumn(normalizedHeaders, SubtotalExGstHeaders);
            var storePriceIdx = FindColumn(normalizedHeaders, StorePriceHeaders);
            var descriptionIdx = FindColumn(normalizedHeaders, DescriptionHeaders);

            // Restock-sheet mode: quantity comes from "Case of N" (the sheet's own Quantity column is
            // the number of cases, not units), and cost is Subtotal (Excl. GST) / N per unit.
            var restockMode = uomIdx != -1 && subtotalExIdx != -1;
            if (restockMode) stockIdx = uomIdx;

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

                var name = CleanName(Get(nameIdx));
                if (string.IsNullOrWhiteSpace(name)) continue;

                // Footer lines in a sheet (e.g. "INVOICE TOTAL", no unit of measure) are not products.
                if (restockMode && string.IsNullOrWhiteSpace(Get(uomIdx))) continue;

                // An explicit Category column (if the sheet has one) always wins over a guess.
                var explicitCategory = categoryIdx != -1 ? NormalizeCategory(Get(categoryIdx)) : "";
                var category = string.IsNullOrEmpty(explicitCategory) ? GuessCategory(name) : explicitCategory;
                var isPublished = publishedIdx != -1 && Get(publishedIdx).Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

                var piecesPerCase = restockMode ? ParsePiecesPerCase(Get(uomIdx), Get(piecesIdx)) : 0;

                // Every column from the file, in its original order and with its original header
                // text. Columns that also play a role are editable and feed the import payload.
                var columns = new List<ImportColumnDto>();
                for (var i = 0; i < headers.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(headers[i])) continue;

                    // The sheet's own "Quantity" is the number of cases (always 1) and is never used -
                    // hide it so it can not be mistaken for the real unit quantity.
                    if (restockMode && i != uomIdx && StockHeaders.Contains(normalizedHeaders[i])) continue;

                    var value = FormatExtraValue(Get(i));
                    var columnName = headers[i];
                    if (i == nameIdx) value = name;
                    else if (restockMode && i == uomIdx)
                    {
                        value = piecesPerCase.ToString(CultureInfo.InvariantCulture);
                        columnName = "Quantity (units)";
                    }
                    columns.Add(new ImportColumnDto { Name = columnName, Value = value, Role = RoleFor(i) });
                }

                var price = 0m;
                if (restockMode)
                {
                    // Cost per unit, GST-exclusive - the system adds GST itself (see PricingCalculator).
                    if (costIdx == -1)
                    {
                        var unitCost = TryParseDecimal(Get(subtotalExIdx), out var sub) && piecesPerCase > 0
                            ? Math.Round(sub / piecesPerCase, 2) : 0m;
                        columns.Add(new ImportColumnDto { Name = "Unit Cost (Excl. GST)", Value = unitCost.ToString("0.00", CultureInfo.InvariantCulture), Role = "cost" });
                    }
                    if (marginIdx == -1)
                        columns.Add(new ImportColumnDto { Name = "Margin", Value = DefaultMargin.ToString("0.00", CultureInfo.InvariantCulture), Role = "margin" });

                    // The sheet's own retail price (GST-inclusive) becomes the store price as given.
                    if (storePriceIdx != -1 && TryParseDecimal(Get(storePriceIdx), out var retail))
                        price = Math.Round(retail, 2);
                }

                rows.Add(new ImportPreviewRowDto
                {
                    Columns = columns,
                    Category = category,
                    CategoryGuessed = string.IsNullOrEmpty(explicitCategory) && !string.IsNullOrEmpty(category),
                    Price = price, // 0 unless the sheet carries its own retail price
                    IsPublished = isPublished,
                    ShortDescription = descriptionIdx != -1 ? Get(descriptionIdx).Trim() : "",
                });
            }

            return rows;
        }
    }
}
