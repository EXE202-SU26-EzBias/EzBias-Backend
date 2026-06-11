using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EzBias.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRatingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ratings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ratings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    buyer_id = table.Column<long>(type: "bigint", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    seller_id = table.Column<long>(type: "bigint", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    product_rating = table.Column<short>(type: "smallint", nullable: false),
                    seller_rating = table.Column<short>(type: "smallint", nullable: false),
                    tags = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ratings", x => x.id);
                    table.CheckConstraint("ck_ratings_product_rating", "product_rating >= 1 AND product_rating <= 5");
                    table.CheckConstraint("ck_ratings_seller_rating", "seller_rating >= 1 AND seller_rating <= 5");
                    table.ForeignKey(
                        name: "FK_ratings_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ratings_users_buyer_id",
                        column: x => x.buyer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ratings_users_seller_id",
                        column: x => x.seller_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ratings_buyer_id",
                table: "ratings",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "IX_ratings_order_id",
                table: "ratings",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ratings_seller_id",
                table: "ratings",
                column: "seller_id");
        }
    }
}
