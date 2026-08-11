using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Modules.Wiki.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPageFavoritesAndPins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPageFavorites",
                schema: "wiki",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WikiPageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPageFavorites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPagePins",
                schema: "wiki",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WikiPageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPagePins", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPageFavorites_UserId_WikiPageId",
                schema: "wiki",
                table: "UserPageFavorites",
                columns: new[] { "UserId", "WikiPageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPagePins_UserId_WikiPageId",
                schema: "wiki",
                table: "UserPagePins",
                columns: new[] { "UserId", "WikiPageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPageFavorites",
                schema: "wiki");

            migrationBuilder.DropTable(
                name: "UserPagePins",
                schema: "wiki");
        }
    }
}
