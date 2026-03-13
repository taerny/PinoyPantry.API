using Microsoft.EntityFrameworkCore;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Data
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Lucky Me Pancit Canton Original", Description = "Classic Filipino instant noodles with savory sauce.", Price = 1.50m, ImageUrl = "", Category = "Noodles", StockQuantity = 100, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Product { Id = 2, Name = "Datu Puti Sukang Maasim", Description = "Filipino cane vinegar, essential for dipping sauces.", Price = 2.99m, ImageUrl = "", Category = "Condiments", StockQuantity = 80, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Product { Id = 3, Name = "Silver Swan Soy Sauce", Description = "All-purpose Filipino soy sauce for cooking and dipping.", Price = 3.49m, ImageUrl = "", Category = "Condiments", StockQuantity = 90, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Product { Id = 4, Name = "Jufran Banana Ketchup", Description = "Sweet and tangy banana ketchup, a Filipino pantry staple.", Price = 2.75m, ImageUrl = "", Category = "Condiments", StockQuantity = 70, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Product { Id = 5, Name = "Knorr Sinigang Mix", Description = "Tamarind soup base mix for the classic sinigang dish.", Price = 1.99m, ImageUrl = "", Category = "Soups & Mixes", StockQuantity = 120, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Product { Id = 6, Name = "Mang Tomas All-Around Sarsa", Description = "Filipino liver sauce perfect for lechon and grilled meats.", Price = 3.25m, ImageUrl = "", Category = "Condiments", StockQuantity = 60, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Product { Id = 7, Name = "Spam Lite", Description = "Lower sodium canned pork, popular for Pinoy breakfast.", Price = 4.99m, ImageUrl = "", Category = "Canned Goods", StockQuantity = 50, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Product { Id = 8, Name = "Century Tuna Flakes in Oil", Description = "Canned tuna flakes in oil, great for pasta and rice dishes.", Price = 2.50m, ImageUrl = "", Category = "Canned Goods", StockQuantity = 110, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Product { Id = 9, Name = "Skyflakes Crackers", Description = "Light and crispy crackers, a classic Filipino snack.", Price = 1.75m, ImageUrl = "", Category = "Snacks", StockQuantity = 200, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Product { Id = 10, Name = "Bear Brand Sterilized Milk", Description = "Full cream sterilized milk, long-life pantry staple.", Price = 1.25m, ImageUrl = "", Category = "Dairy", StockQuantity = 150, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        }
    }
}
