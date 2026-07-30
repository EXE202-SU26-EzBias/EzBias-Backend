using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EzBias.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenAuctionBidConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bids_auction_id",
                table: "bids");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "auctions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.Sql("""
                WITH ranked AS (
                    SELECT id,
                           ROW_NUMBER() OVER (
                               PARTITION BY auction_id
                               ORDER BY amount DESC, placed_at ASC, id ASC
                           ) AS row_number
                    FROM bids
                    WHERE is_winning = TRUE
                )
                UPDATE bids
                SET is_winning = FALSE
                WHERE id IN (
                    SELECT id
                    FROM ranked
                    WHERE row_number > 1
                );
                """);

            migrationBuilder.CreateIndex(
                name: "uq_bids_one_winning_per_auction",
                table: "bids",
                column: "auction_id",
                unique: true,
                filter: "is_winning = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_bids_one_winning_per_auction",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "auctions");

            migrationBuilder.CreateIndex(
                name: "IX_bids_auction_id",
                table: "bids",
                column: "auction_id");
        }
    }
}
