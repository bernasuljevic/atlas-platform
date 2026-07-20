using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Modules.Wiki.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWikiPageUserForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Wiki.Domain, Auth.Domain'i tanimadigi icin EF Core burada otomatik bir
            // iliski kuramiyor - kisitlamayi ham SQL ile ekliyoruz. Bu, mimarimizin
            // "modul kod seviyesinde ayrik, veritabani seviyesinde tutarli" ilkesinin
            // somut karsiligi.
            migrationBuilder.Sql(@"
                ALTER TABLE [wiki].[WikiPages]
                ADD CONSTRAINT [FK_WikiPages_Users_CreatedByUserId]
                FOREIGN KEY ([CreatedByUserId])
                REFERENCES [auth].[Users] ([Id]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down() - migration geri alinirsa (ef migrations remove / database update
            // ile eski bir migration'a donulurse) kisitlamayi kaldiran ters islem.
            migrationBuilder.Sql(@"
                ALTER TABLE [wiki].[WikiPages]
                DROP CONSTRAINT [FK_WikiPages_Users_CreatedByUserId];
            ");
        }
    }
}
