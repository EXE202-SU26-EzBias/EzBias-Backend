using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EzBias.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductReviewMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_review_media",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_review_id = table.Column<long>(type: "bigint", nullable: false),
                    media_type = table.Column<short>(type: "smallint", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    thumbnail_url = table.Column<string>(type: "text", nullable: true),
                    cloudinary_public_id = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_review_media", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_review_media_product_reviews_product_review_id",
                        column: x => x.product_review_id,
                        principalTable: "product_reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_review_media_product_review_id_sort_order",
                table: "product_review_media",
                columns: new[] { "product_review_id", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_review_media");
        }
    }
}
