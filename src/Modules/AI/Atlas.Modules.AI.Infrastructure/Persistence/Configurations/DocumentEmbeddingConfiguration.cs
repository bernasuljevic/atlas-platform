using Atlas.Modules.AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.AI.Infrastructure.Persistence.Configurations;

// WikiPageEmbeddingConfiguration'ın BİREBİR kopyası - DocumentId üzerinden.
public class DocumentEmbeddingConfiguration : IEntityTypeConfiguration<DocumentEmbedding>
{
    public void Configure(EntityTypeBuilder<DocumentEmbedding> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.DocumentId)
            .IsRequired();

        builder.Property(e => e.ChunkIndex)
            .IsRequired();

        builder.Property(e => e.ChunkText)
            .IsRequired();

        builder.Property(e => e.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.DepartmentName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Visibility)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Embedding)
            .HasColumnType($"vector({DocumentEmbedding.EmbeddingDimension})")
            .IsRequired();

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(e => new { e.DocumentId, e.ChunkIndex });
    }
}
