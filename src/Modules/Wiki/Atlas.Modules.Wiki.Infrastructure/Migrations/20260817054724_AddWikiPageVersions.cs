using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Modules.Wiki.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWikiPageVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue: 1 (0 DEĞİL) - migration'dan ÖNCE var olan sayfalar
            // hiç düzenlenmemiş olsa da "1. versiyon"da sayılır, WikiPage.cs'teki
            // in-memory varsayılanla (= 1) tutarlı olsun diye.
            migrationBuilder.AddColumn<int>(
                name: "CurrentVersionNumber",
                schema: "wiki",
                table: "WikiPages",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "WikiPageVersions",
                schema: "wiki",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WikiPageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Visibility = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EditedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EditedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiPageVersions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WikiPageVersions_WikiPageId_VersionNumber",
                schema: "wiki",
                table: "WikiPageVersions",
                columns: new[] { "WikiPageId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WikiPageVersions",
                schema: "wiki");

            migrationBuilder.DropColumn(
                name: "CurrentVersionNumber",
                schema: "wiki",
                table: "WikiPages");
        }
    }
}
