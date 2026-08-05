using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Modules.Wiki.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWikiPageCreatedByEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByEmail",
                schema: "wiki",
                table: "WikiPages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByEmail",
                schema: "wiki",
                table: "WikiPages");
        }
    }
}
