using Atlas.Modules.AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.AI.Infrastructure.Persistence.Configurations;

public class WikiPageEmbeddingConfiguration : IEntityTypeConfiguration<WikiPageEmbedding>
{
    public void Configure(EntityTypeBuilder<WikiPageEmbedding> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.WikiPageId)
            .IsRequired();

        // Voyage AI'ın embedding boyutu 1024 - "vector(1024)" sütun tipi bunu
        // veritabanı seviyesinde de sabitliyor (yanlış boyutlu bir embedding
        // eklenmeye çalışılırsa PostgreSQL isteği reddeder).
        builder.Property(e => e.Embedding)
            .HasColumnType("vector(1024)")
            .IsRequired();

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        // Bir wiki sayfasının embedding'ini ararken sık kullanılacak.
        builder.HasIndex(e => e.WikiPageId);
    }
}
