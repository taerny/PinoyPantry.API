using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PinoyPantry.API.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_FixImageUrlAndSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrll",
                table: "Products",
                newName: "ImageUrl");

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "ImageUrl", "Name", "Price", "StockQuantity" },
                values: new object[,]
                {
                    { 1, "Noodles", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Classic Filipino instant noodles with savory sauce.", "", "Lucky Me Pancit Canton Original", 1.50m, 100 },
                    { 2, "Condiments", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Filipino cane vinegar, essential for dipping sauces.", "", "Datu Puti Sukang Maasim", 2.99m, 80 },
                    { 3, "Condiments", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "All-purpose Filipino soy sauce for cooking and dipping.", "", "Silver Swan Soy Sauce", 3.49m, 90 },
                    { 4, "Condiments", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sweet and tangy banana ketchup, a Filipino pantry staple.", "", "Jufran Banana Ketchup", 2.75m, 70 },
                    { 5, "Soups & Mixes", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tamarind soup base mix for the classic sinigang dish.", "", "Knorr Sinigang Mix", 1.99m, 120 },
                    { 6, "Condiments", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Filipino liver sauce perfect for lechon and grilled meats.", "", "Mang Tomas All-Around Sarsa", 3.25m, 60 },
                    { 7, "Canned Goods", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lower sodium canned pork, popular for Pinoy breakfast.", "", "Spam Lite", 4.99m, 50 },
                    { 8, "Canned Goods", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Canned tuna flakes in oil, great for pasta and rice dishes.", "", "Century Tuna Flakes in Oil", 2.50m, 110 },
                    { 9, "Snacks", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Light and crispy crackers, a classic Filipino snack.", "", "Skyflakes Crackers", 1.75m, 200 },
                    { 10, "Dairy", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Full cream sterilized milk, long-life pantry staple.", "", "Bear Brand Sterilized Milk", 1.25m, 150 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Products",
                newName: "ImageUrll");
        }
    }
}
