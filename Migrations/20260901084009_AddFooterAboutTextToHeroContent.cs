using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PinoyPantry.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFooterAboutTextToHeroContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FooterAboutText",
                table: "HeroContents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "HeroContents",
                keyColumn: "Id",
                keyValue: 1,
                column: "FooterAboutText",
                value: "Your one-stop shop for authentic Filipino foods. Bringing the taste of home to you!");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FooterAboutText",
                table: "HeroContents");
        }
    }
}
