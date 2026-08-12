using Atlas.Modules.Documents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Documents.Infrastructure.Persistence.Configurations;

public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.DocumentId).IsRequired();
        builder.Property(v => v.VersionNumber).IsRequired();

        builder.Property(v => v.OriginalFileName).IsRequired().HasMaxLength(300);
        builder.Property(v => v.StorageKey).IsRequired().HasMaxLength(260);
        builder.Property(v => v.ContentType).IsRequired().HasMaxLength(150);
        builder.Property(v => v.FileExtension).IsRequired().HasMaxLength(20);
        builder.Property(v => v.ContentHash).IsRequired().HasMaxLength(64);

        builder.Property(v => v.CreatedByEmail).HasMaxLength(256);

        // Bir belgenin versiyon geçmişini SIRALI çekmek için (DocumentId, VersionNumber) -
        // WikiPageEmbedding/DocumentEmbedding'deki AYNI composite index deseni.
        builder.HasIndex(v => new { v.DocumentId, v.VersionNumber }).IsUnique();
    }
}
