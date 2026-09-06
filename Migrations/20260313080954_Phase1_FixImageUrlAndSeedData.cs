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

            // Seed insert skipped: this DB already has real product rows (ids 2-11)
            // that conflict on primary key with the original sample seed (ids 1-10).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Products",
                newName: "ImageUrll");
        }
    }
}
