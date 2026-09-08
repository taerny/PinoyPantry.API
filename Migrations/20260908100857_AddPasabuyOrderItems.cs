using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PinoyPantry.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPasabuyOrderItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ItemsRequested",
                table: "PasabuyOrders",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "PasabuyOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PasabuyOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasabuyOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasabuyOrderItems_PasabuyOrders_PasabuyOrderId",
                        column: x => x.PasabuyOrderId,
                        principalTable: "PasabuyOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasabuyOrderItems_PasabuyOrderId",
                table: "PasabuyOrderItems",
                column: "PasabuyOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasabuyOrderItems");

            migrationBuilder.AlterColumn<string>(
                name: "ItemsRequested",
                table: "PasabuyOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
