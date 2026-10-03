using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PinoyPantry.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAmountPaidToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: true);

            // Orders couldn't be corrected before this migration, so every already-paid
            // order's Total is exactly what the customer paid.
            migrationBuilder.Sql(
                "UPDATE Orders SET AmountPaid = Total WHERE Status IN ('Paid', 'Completed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "Orders");
        }
    }
}
