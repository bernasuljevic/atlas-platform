using Atlas.Modules.Vault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Vault.Infrastructure.Persistence.Configurations;

public class PasswordEntryConfiguration : IEntityTypeConfiguration<PasswordEntry>
{
    public void Configure(EntityTypeBuilder<PasswordEntry> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Username)
            .HasMaxLength(200);

        // Uzunluk sınırı BİLEREK yok - Data Protection'ın ürettiği ciphertext,
        // düz metin parolanın kendisinden belirgin şekilde daha uzun (IV/tag/
        // versiyon bilgisi içeriyor), sabit bir üst sınır koymak ileride kırılgan
        // olurdu. nvarchar(max)'a düşmesi burada sorun değil.
        builder.Property(p => p.EncryptedPassword)
            .IsRequired();

        builder.Property(p => p.Url).HasMaxLength(500);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Category).HasMaxLength(100);
        builder.Property(p => p.Notes).HasMaxLength(2000);

        builder.Property(p => p.CreatedByEmail).HasMaxLength(256);

        // "Kimin oluşturduğu" hem yetki kontrolü (bkz. Handler'lardaki owner
        // kontrolü) hem "benim kayıtlarım" listesi için sık sorgulanacak.
        builder.HasIndex(p => p.CreatedByUserId);

        // Kategori filtreleme (spec'in "Category" özelliği) için.
        builder.HasIndex(p => p.Category);
    }
}
