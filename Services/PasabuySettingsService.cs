using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PinoyPantry.API.Data;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Services
{
    // Loads the single Pasabuy settings row (falling back to the original hardcoded wording when
    // nothing has been saved yet) and works out whether orders are currently open.
    public static class PasabuySettingsService
    {
        private static readonly TimeZoneInfo NzTimeZone = FindNzTimeZone();

        private static TimeZoneInfo FindNzTimeZone()
        {
            foreach (var id in new[] { "New Zealand Standard Time", "Pacific/Auckland" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch (TimeZoneNotFoundException) { }
            }
            return TimeZoneInfo.Utc;
        }

        public static DateTime TodayNz() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, NzTimeZone).Date;

        public static PasabuySettings Defaults() => new()
        {
            BadgeText = "🇵🇭 PASABUY — ORDERS NOW OPEN 🇳🇿",
            ClosedBadgeText = "🇵🇭 PASABUY — ORDERS CLOSED 🇳🇿",
            Heading = "Filipino Grocery Pasabuy",
            Intro = "Missing your favourite Filipino snacks, biscuits, canned goods and pantry essentials?\nWe're now taking orders for our next Filipino grocery shipment! 🇵🇭❤️",
            CutoffDate = new DateTime(2026, 9, 20),
            EstimatedArrival = "Last week of September 2026",
            Collection = "Dunedin, NZ",
            FormTitle = "Send Us Your Order",
            FormBlurb = "Limited shipment capacity available, so get your orders in before {cutoff}! 🇵🇭❤️",
            HowItWorks = string.Join("\n", new[]
            {
                "Think of a product that you want.",
                "Send us your order.",
                "Payment required upon delivery/pick up.",
                "We purchase your items in the Philippines and arrange shipping to New Zealand.",
                "Once your order arrives in Dunedin, we'll contact you regarding collection/delivery.",
            }),
            PricingText = "Our prices are calculated to include the product cost, international freight and anticipated import/clearance costs. Additional charges may apply in exceptional circumstances (e.g. unforeseen customs/MPI inspection costs) — if any unexpected charge affects your order, we'll contact you before proceeding.",
            PleaseNote = string.Join("\n", new[]
            {
                "Orders are subject to product availability in the Philippines.",
                "If an item becomes unavailable, we will contact you regarding a suitable alternative.",
                "The arrival date is an estimated ETA, not a guaranteed delivery date. Shipping schedules, customs clearance and MPI processing can sometimes cause delays.",
                "Once your order has been submitted, cancellations may not be possible as we purchase items specifically for each customer.",
                "Please check your order carefully before submitting it.",
            }),
            ClosedMessage = "Orders for this shipment are now closed. Our next shipment is coming soon — follow us on Facebook for updates! 🇵🇭❤️",
            ForceClosed = false,
        };

        public static async Task<PasabuySettings> LoadAsync(ApplicationDBContext context) =>
            await context.PasabuySettings.AsNoTracking().FirstOrDefaultAsync() ?? Defaults();

        // Open until the end of the cut-off day (NZ time), unless manually closed.
        public static bool IsOpen(PasabuySettings s) => !s.ForceClosed && TodayNz() <= s.CutoffDate.Date;

        private static List<string> Lines(string s) =>
            s.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

        public static PasabuySettingsDto ToDto(PasabuySettings s) => new()
        {
            BadgeText = s.BadgeText,
            ClosedBadgeText = s.ClosedBadgeText,
            Heading = s.Heading,
            Intro = s.Intro,
            CutoffDate = s.CutoffDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            EstimatedArrival = s.EstimatedArrival,
            Collection = s.Collection,
            FormTitle = s.FormTitle,
            FormBlurb = s.FormBlurb,
            HowItWorks = Lines(s.HowItWorks),
            PricingText = s.PricingText,
            PleaseNote = Lines(s.PleaseNote),
            ClosedMessage = s.ClosedMessage,
            ForceClosed = s.ForceClosed,
            IsOpen = IsOpen(s),
            CutoffDisplay = s.CutoffDate.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("en-NZ")),
        };

        public static void Apply(PasabuySettings s, PasabuySettingsDto dto, DateTime cutoff)
        {
            s.BadgeText = dto.BadgeText.Trim();
            s.ClosedBadgeText = dto.ClosedBadgeText.Trim();
            s.Heading = dto.Heading.Trim();
            s.Intro = dto.Intro.Trim();
            s.CutoffDate = cutoff.Date;
            s.EstimatedArrival = dto.EstimatedArrival.Trim();
            s.Collection = dto.Collection.Trim();
            s.FormTitle = dto.FormTitle.Trim();
            s.FormBlurb = dto.FormBlurb.Trim();
            s.HowItWorks = string.Join("\n", dto.HowItWorks.Select(l => l.Trim()).Where(l => l.Length > 0));
            s.PricingText = dto.PricingText.Trim();
            s.PleaseNote = string.Join("\n", dto.PleaseNote.Select(l => l.Trim()).Where(l => l.Length > 0));
            s.ClosedMessage = dto.ClosedMessage.Trim();
            s.ForceClosed = dto.ForceClosed;
            s.UpdatedAt = DateTime.UtcNow;
        }
    }
}
