using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Modules.AI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchFieldsToWikiPageEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepartmentName",
                schema: "ai",
                table: "WikiPageEmbeddings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "ai",
                table: "WikiPageEmbeddings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                schema: "ai",
                table: "WikiPageEmbeddings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartmentName",
                schema: "ai",
                table: "WikiPageEmbeddings");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "ai",
                table: "WikiPageEmbeddings");

            migrationBuilder.DropColumn(
                name: "Visibility",
                schema: "ai",
                table: "WikiPageEmbeddings");
        }
    }
}
