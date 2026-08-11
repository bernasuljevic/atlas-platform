using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Modules.Vault.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByEmail",
                schema: "vault",
                table: "PasswordEntries",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByEmail",
                schema: "vault",
                table: "PasswordEntries");
        }
    }
}
