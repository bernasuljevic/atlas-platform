using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence.Configurations;

public class WikiPageVersionConfiguration : IEntityTypeConfiguration<WikiPageVersion>
{
    public void Configure(EntityTypeBuilder<WikiPageVersion> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Title).IsRequired().HasMaxLength(200);
        builder.Property(v => v.Content).IsRequired();
        builder.Property(v => v.Visibility).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Tags).HasMaxLength(300);
        builder.Property(v => v.EditedByEmail).HasMaxLength(256);

        // "Bu sayfanın versiyon geçmişi" sorgusu ("WikiPageId = X, en yeniden
        // en eskiye sırala") çok sık çalışacak - composite index WikiPage'in
        // (WikiPageId, ChunkIndex) desenindeki AYNI gerekçe.
        builder.HasIndex(v => new { v.WikiPageId, v.VersionNumber }).IsUnique();
    }
}
