using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence.Configurations;

public class WikiFolderConfiguration : IEntityTypeConfiguration<WikiFolder>
{
    public void Configure(EntityTypeBuilder<WikiFolder> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.DepartmentName)
            .IsRequired()
            .HasMaxLength(100);

        // Kendi kendine referans veren ilişki (bir klasörün üst klasörü de bir
        // WikiFolder). Restrict BİLİNÇLİ bir seçim: içinde alt klasör varken bir
        // üst klasörün silinmesini engelliyor - aksi halde (Cascade) bir üst
        // klasörü silmek tüm alt ağacı sessizce yok ederdi.
        builder.HasOne<WikiFolder>()
            .WithMany()
            .HasForeignKey(f => f.ParentFolderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bir departmanın klasör ağacını (kök klasörler + belirli bir klasörün
        // alt klasörleri) çekmek sık kullanılacak - bileşik index.
        builder.HasIndex(f => new { f.DepartmentName, f.ParentFolderId });
    }
}
