using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PinoyPantry.API.Migrations
{
    /// <inheritdoc />
    public partial class AddHeroContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HeroContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Headline = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HighlightedText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subtext = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ButtonText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ButtonLink = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeroContents", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "HeroContents",
                columns: new[] { "Id", "ButtonLink", "ButtonText", "Headline", "HighlightedText", "Subtext", "UpdatedAt" },
                values: new object[] { 1, "/category/all-products", "Shop Now", "Real Filipino Flavours", "From Our Pantry to Yours", "From classic canned goods to your favorite snacks — everything you need to bring the taste of home to your kitchen.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeroContents");
        }
    }
}
