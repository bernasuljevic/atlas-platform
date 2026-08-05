using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence.Configurations;

public class WikiPageConfiguration : IEntityTypeConfiguration<WikiPage>
{
    public void Configure(EntityTypeBuilder<WikiPage> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Content)
            .IsRequired();

        builder.Property(p => p.DepartmentName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Tags)
            .HasMaxLength(300);

        // Visibility bir enum (Public/DepartmentOnly) - EF Core varsayılan olarak
        // bunu int (0/1) olarak saklar. Şimdilik yeterli; SSMS'te sayı görürsün.
        builder.Property(p => p.Visibility)
            .IsRequired();

        // department + visibility'ye göre filtreleme sık kullanılacağı için index ekliyoruz.
        builder.HasIndex(p => new { p.DepartmentName, p.Visibility });

        // SetNull BİLİNÇLİ bir seçim: bir klasör silinirse içindeki sayfalar
        // SİLİNMEZ, sadece "dosyalanmamış" (FolderId=null) duruma düşer - bir
        // sayfanın kategorisini kaybetmesi geri alınabilir/düşük riskli, bir
        // klasörü silmenin içindeki sayfaları da silmesi ise şaşırtıcı ve
        // yıkıcı olurdu.
        builder.HasOne<WikiFolder>()
            .WithMany()
            .HasForeignKey(p => p.FolderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.FolderId);
    }
}
