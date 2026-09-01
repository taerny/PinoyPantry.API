using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PinoyPantry.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceModeToHeroContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMaintenanceMode",
                table: "HeroContents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceHeadline",
                table: "HeroContents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceMessage",
                table: "HeroContents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "HeroContents",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "IsMaintenanceMode", "MaintenanceHeadline", "MaintenanceMessage" },
                values: new object[] { false, "We're Cooking Up Something New!", "PinoyPantry is getting a fresh batch of updates. Balik kami agad — hang tight, we'll be back before you can say 'Pasabuy!'" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMaintenanceMode",
                table: "HeroContents");

            migrationBuilder.DropColumn(
                name: "MaintenanceHeadline",
                table: "HeroContents");

            migrationBuilder.DropColumn(
                name: "MaintenanceMessage",
                table: "HeroContents");
        }
    }
}
