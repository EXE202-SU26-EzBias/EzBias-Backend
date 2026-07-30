using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EzBias.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameReviewMediaStoragePublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "cloudinary_public_id",
                table: "product_review_media",
                newName: "storage_public_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "storage_public_id",
                table: "product_review_media",
                newName: "cloudinary_public_id");
        }
    }
}
