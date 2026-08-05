using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Modules.Wiki.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWikiFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FolderId",
                schema: "wiki",
                table: "WikiPages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WikiFolders",
                schema: "wiki",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParentFolderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WikiFolders_WikiFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalSchema: "wiki",
                        principalTable: "WikiFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WikiPages_FolderId",
                schema: "wiki",
                table: "WikiPages",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_WikiFolders_DepartmentName_ParentFolderId",
                schema: "wiki",
                table: "WikiFolders",
                columns: new[] { "DepartmentName", "ParentFolderId" });

            migrationBuilder.CreateIndex(
                name: "IX_WikiFolders_ParentFolderId",
                schema: "wiki",
                table: "WikiFolders",
                column: "ParentFolderId");

            migrationBuilder.AddForeignKey(
                name: "FK_WikiPages_WikiFolders_FolderId",
                schema: "wiki",
                table: "WikiPages",
                column: "FolderId",
                principalSchema: "wiki",
                principalTable: "WikiFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WikiPages_WikiFolders_FolderId",
                schema: "wiki",
                table: "WikiPages");

            migrationBuilder.DropTable(
                name: "WikiFolders",
                schema: "wiki");

            migrationBuilder.DropIndex(
                name: "IX_WikiPages_FolderId",
                schema: "wiki",
                table: "WikiPages");

            migrationBuilder.DropColumn(
                name: "FolderId",
                schema: "wiki",
                table: "WikiPages");
        }
    }
}
