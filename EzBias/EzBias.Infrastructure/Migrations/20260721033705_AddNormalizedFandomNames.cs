using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EzBias.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedFandomNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "normalized_name",
                table: "fandoms",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE fandoms
                SET normalized_name = lower(
                    btrim(regexp_replace(normalize(name, NFKC), '\s+', ' ', 'g'))
                );

                WITH ranked AS (
                    SELECT
                        id,
                        first_value(id) OVER (
                            PARTITION BY normalized_name
                            ORDER BY created_at, id
                        ) AS canonical_id
                    FROM fandoms
                ), duplicates AS (
                    SELECT id, canonical_id
                    FROM ranked
                    WHERE id <> canonical_id
                )
                UPDATE products AS product
                SET fandom_id = duplicate.canonical_id
                FROM duplicates AS duplicate
                WHERE product.fandom_id = duplicate.id;

                WITH ranked AS (
                    SELECT
                        id,
                        first_value(id) OVER (
                            PARTITION BY normalized_name
                            ORDER BY created_at, id
                        ) AS canonical_id
                    FROM fandoms
                )
                DELETE FROM fandoms AS fandom
                USING ranked
                WHERE fandom.id = ranked.id
                  AND ranked.id <> ranked.canonical_id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_name",
                table: "fandoms",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_fandoms_normalized_name",
                table: "fandoms",
                column: "normalized_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_fandoms_normalized_name",
                table: "fandoms");

            migrationBuilder.DropColumn(
                name: "normalized_name",
                table: "fandoms");
        }
    }
}
