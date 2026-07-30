using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EzBias.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDispatchOutboxFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "dispatch_attempts",
                table: "notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "dispatch_failed_at",
                table: "notifications",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "dispatch_lease_id",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "dispatch_locked_until",
                table: "notifications",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "dispatched_at",
                table: "notifications",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_dispatch_error",
                table: "notifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_dispatch_at",
                table: "notifications",
                type: "timestamptz",
                nullable: true);

            // Existing notifications were already delivered by the previous
            // post-save dispatcher. Do not replay historical rows after rollout.
            migrationBuilder.Sql(
                "UPDATE notifications SET dispatched_at = created_at WHERE dispatched_at IS NULL;");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_dispatch_pending",
                table: "notifications",
                columns: new[] { "next_dispatch_at", "created_at" },
                filter: "dispatched_at IS NULL AND dispatch_failed_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_notifications_dispatch_pending",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "dispatch_attempts",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "dispatch_failed_at",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "dispatch_lease_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "dispatch_locked_until",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "dispatched_at",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "last_dispatch_error",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "next_dispatch_at",
                table: "notifications");
        }
    }
}
