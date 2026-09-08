using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PinoyPantry.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCostPriceToProductBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "ProductBatches",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "ProductBatches",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // Backfill: existing batches inherit their product's current CostPrice, so pre-batch
            // data isn't left showing $0 cost once this column starts being used for real.
            migrationBuilder.Sql(@"
                UPDATE b
                SET b.CostPrice = p.CostPrice,
                    b.Subtotal = p.CostPrice * b.Quantity
                FROM [ProductBatches] b
                JOIN [Products] p ON p.Id = b.ProductId;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "ProductBatches");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "ProductBatches");
        }
    }
}
