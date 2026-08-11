using Atlas.Modules.Documents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Documents.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title).IsRequired().HasMaxLength(200);
        builder.Property(d => d.OriginalFileName).IsRequired().HasMaxLength(300);

        // "{GUID:N}.{uzantı}" formatı en fazla ~40 karakter ama ileride farklı
        // bir storage sağlayıcısına (bkz. IFileStorageService'in DI-swap
        // amacı) geçilirse anahtar biçimi değişebilir - rahat bir üst sınır.
        builder.Property(d => d.StorageKey).IsRequired().HasMaxLength(260);

        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(150);
        builder.Property(d => d.FileExtension).IsRequired().HasMaxLength(20);
        builder.Property(d => d.DepartmentName).IsRequired().HasMaxLength(100);
        builder.Property(d => d.CreatedByEmail).HasMaxLength(256);
        builder.Property(d => d.Description).HasMaxLength(1000);
        builder.Property(d => d.Tags).HasMaxLength(300);
        builder.Property(d => d.ProcessingError).HasMaxLength(2000);

        // SHA-256 hex string HER ZAMAN 64 karakter.
        builder.Property(d => d.ContentHash).IsRequired().HasMaxLength(64);

        // "Benim belgelerim" listesi/yetki kontrolü için (Vault'un AYNI CreatedByUserId
        // index'i).
        builder.HasIndex(d => d.CreatedByUserId);

        // Document Library'nin departman filtresi için.
        builder.HasIndex(d => d.DepartmentName);

        // P6'da duplicate-detection için (aynı departmanda aynı hash var mı) -
        // şimdiden index açmak ucuz, büyümüş bir tabloda sonradan eklemek daha
        // maliyetli olurdu.
        builder.HasIndex(d => d.ContentHash);
    }
}
