using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Modules.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue BİLEREK true - bu SADECE bu migration'ın ALTER TABLE'ı
            // ÇALIŞIRKEN var olan (henüz doğrulama akışından hiç geçmemiş) satırları
            // geriye dönük "doğrulanmış" say, yoksa bu özellik yayına girdiği anda
            // önceden kayıtlı TÜM kullanıcılar (seed edilen admin dahil) aniden
            // giriş yapamaz hale gelirdi. Model tarafında (UserConfiguration.cs)
            // BİLEREK HasDefaultValue YOK - User.Create(..., emailVerified: false)
            // ile oluşturulan YENİ kullanıcılar bu SQL-seviyesi varsayılandan
            // etkilenmiyor, EF her zaman kendi açık değerini INSERT ediyor.
            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                schema: "auth",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "EmailVerificationCodes",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationCodes_UserId_UsedAtUtc",
                schema: "auth",
                table: "EmailVerificationCodes",
                columns: new[] { "UserId", "UsedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailVerificationCodes",
                schema: "auth");

            migrationBuilder.DropColumn(
                name: "EmailVerified",
                schema: "auth",
                table: "Users");
        }
    }
}
