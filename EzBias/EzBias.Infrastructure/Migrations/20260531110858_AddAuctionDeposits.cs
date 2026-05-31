using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EzBias.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionDeposits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "required_deposit_amount",
                table: "auctions",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "auction_deposits",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    auction_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false, defaultValue: "PendingPayment"),
                    payment_id = table.Column<long>(type: "bigint", nullable: true),
                    refund_id = table.Column<long>(type: "bigint", nullable: true),
                    held_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    forfeited_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    refunded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    confirmation_notification_delivered = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    forfeit_retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auction_deposits", x => x.id);
                    table.ForeignKey(
                        name: "FK_auction_deposits_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_auction_deposits_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_auction_deposits_refunds_refund_id",
                        column: x => x.refund_id,
                        principalTable: "refunds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_auction_deposits_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auction_deposits_auction_id_state",
                table: "auction_deposits",
                columns: new[] { "auction_id", "state" });

            migrationBuilder.CreateIndex(
                name: "IX_auction_deposits_payment_id",
                table: "auction_deposits",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_auction_deposits_refund_id",
                table: "auction_deposits",
                column: "refund_id");

            migrationBuilder.CreateIndex(
                name: "uq_active_deposit_per_user_auction",
                table: "auction_deposits",
                columns: new[] { "user_id", "auction_id" },
                unique: true,
                filter: "state IN ('PendingPayment','Held')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auction_deposits");

            migrationBuilder.DropColumn(
                name: "required_deposit_amount",
                table: "auctions");
        }
    }
}
