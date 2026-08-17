using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Modules.Notifications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationTargetingAndReadState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TargetUserId",
                schema: "notifications",
                table: "NotificationEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificationReads",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationReads", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationEntries_TargetUserId",
                schema: "notifications",
                table: "NotificationEntries",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationReads_NotificationEntryId_UserId",
                schema: "notifications",
                table: "NotificationReads",
                columns: new[] { "NotificationEntryId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationReads",
                schema: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_NotificationEntries_TargetUserId",
                schema: "notifications",
                table: "NotificationEntries");

            migrationBuilder.DropColumn(
                name: "TargetUserId",
                schema: "notifications",
                table: "NotificationEntries");
        }
    }
}
