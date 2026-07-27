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

        builder.Property(e => e.ChunkIndex)
            .IsRequired();

        builder.Property(e => e.ChunkText)
            .IsRequired();

        // Sütun tipi Domain'deki tek doğruluk kaynağından (WikiPageEmbedding.
        // EmbeddingDimension) türetiliyor - "1024" sayısı burada AYRICA elle
        // yazılmıyor, ikisi asla birbirinden bağımsız değişemez.
        builder.Property(e => e.Embedding)
            .HasColumnType($"vector({WikiPageEmbedding.EmbeddingDimension})")
            .IsRequired();

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        // Tekil bir WikiPageId index'i yerine (WikiPageId, ChunkIndex) composite
        // index: bir sayfanın chunk'larını SIRALI çekmek için (arama sonucu
        // gösterirken işimize yarayacak). Composite index'in "soldan öncelik"
        // kuralı sayesinde sadece WikiPageId'ye göre filtreleyen sorgular da bu
        // index'i kullanabiliyor - ayrı bir tekil index'e gerek kalmıyor.
        builder.HasIndex(e => new { e.WikiPageId, e.ChunkIndex });
    }
}
