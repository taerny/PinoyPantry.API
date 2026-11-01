using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PinoyPantry.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPasabuySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasabuySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BadgeText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClosedBadgeText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Intro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CutoffDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstimatedArrival = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Collection = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FormTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FormBlurb = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HowItWorks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PricingText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PleaseNote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClosedMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ForceClosed = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasabuySettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasabuySettings");
        }
    }
}
