using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EzBias.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropShippingFeeFromOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "shipping_fee",
                table: "orders");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "auctions",
                type: "text",
                nullable: false,
                defaultValue: "Draft",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Live");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "shipping_fee",
                table: "orders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "auctions",
                type: "text",
                nullable: false,
                defaultValue: "Live",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Draft");
        }
    }
}
