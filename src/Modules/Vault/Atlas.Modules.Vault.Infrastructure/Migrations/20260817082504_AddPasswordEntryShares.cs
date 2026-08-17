using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Modules.Vault.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordEntryShares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasswordEntryShares",
                schema: "vault",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PasswordEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedWithEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SharedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordEntryShares", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordEntryShares_PasswordEntryId_SharedWithUserId",
                schema: "vault",
                table: "PasswordEntryShares",
                columns: new[] { "PasswordEntryId", "SharedWithUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordEntryShares_SharedWithUserId",
                schema: "vault",
                table: "PasswordEntryShares",
                column: "SharedWithUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordEntryShares",
                schema: "vault");
        }
    }
}
