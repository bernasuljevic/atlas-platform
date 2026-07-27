using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Modules.AI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChunkFieldsToWikiPageEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WikiPageEmbeddings_WikiPageId",
                schema: "ai",
                table: "WikiPageEmbeddings");

            migrationBuilder.AddColumn<int>(
                name: "ChunkIndex",
                schema: "ai",
                table: "WikiPageEmbeddings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ChunkText",
                schema: "ai",
                table: "WikiPageEmbeddings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_WikiPageEmbeddings_WikiPageId_ChunkIndex",
                schema: "ai",
                table: "WikiPageEmbeddings",
                columns: new[] { "WikiPageId", "ChunkIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WikiPageEmbeddings_WikiPageId_ChunkIndex",
                schema: "ai",
                table: "WikiPageEmbeddings");

            migrationBuilder.DropColumn(
                name: "ChunkIndex",
                schema: "ai",
                table: "WikiPageEmbeddings");

            migrationBuilder.DropColumn(
                name: "ChunkText",
                schema: "ai",
                table: "WikiPageEmbeddings");

            migrationBuilder.CreateIndex(
                name: "IX_WikiPageEmbeddings_WikiPageId",
                schema: "ai",
                table: "WikiPageEmbeddings",
                column: "WikiPageId");
        }
    }
}
