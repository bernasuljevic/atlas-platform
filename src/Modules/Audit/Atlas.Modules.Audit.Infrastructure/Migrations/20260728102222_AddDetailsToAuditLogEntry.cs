using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Modules.Audit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailsToAuditLogEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Details",
                schema: "audit",
                table: "AuditLogEntries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Details",
                schema: "audit",
                table: "AuditLogEntries");
        }
    }
}
