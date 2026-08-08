using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EzBias.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedFinalizedFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "photocard_details");

            migrationBuilder.DropTable(
                name: "product_boosts");

            migrationBuilder.DropTable(
                name: "seller_follows");

            migrationBuilder.DropTable(
                name: "wishlists");

            migrationBuilder.DropIndex(
                name: "idx_products_boosted",
                table: "products");

            migrationBuilder.DropColumn(
                name: "boost_ends_at",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_boosted",
                table: "products");

            migrationBuilder.DropColumn(
                name: "view_count",
                table: "products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "boost_ends_at",
                table: "products",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_boosted",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "view_count",
                table: "products",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "photocard_details",
                columns: table => new
                {
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    album_series = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    is_pob = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    member_name = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    version = table.Column<string>(type: "text", nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photocard_details", x => x.product_id);
                    table.ForeignKey(
                        name: "FK_photocard_details_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_boosts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_boosts", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_boosts_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_boosts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seller_follows",
                columns: table => new
                {
                    follower_id = table.Column<long>(type: "bigint", nullable: false),
                    seller_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seller_follows", x => new { x.follower_id, x.seller_id });
                    table.ForeignKey(
                        name: "FK_seller_follows_users_follower_id",
                        column: x => x.follower_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_seller_follows_users_seller_id",
                        column: x => x.seller_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wishlists",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wishlists", x => new { x.user_id, x.product_id });
                    table.ForeignKey(
                        name: "FK_wishlists_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_wishlists_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_products_boosted",
                table: "products",
                columns: new[] { "is_boosted", "boost_ends_at" });

            migrationBuilder.CreateIndex(
                name: "idx_boosts_active_product",
                table: "product_boosts",
                columns: new[] { "product_id", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "idx_boosts_expiry_scan",
                table: "product_boosts",
                column: "ends_at");

            migrationBuilder.CreateIndex(
                name: "IX_product_boosts_user_id",
                table: "product_boosts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_seller_follows_seller_id",
                table: "seller_follows",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_wishlists_product_id",
                table: "wishlists",
                column: "product_id");
        }
    }
}
