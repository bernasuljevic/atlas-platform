using Atlas.Modules.Audit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Audit.Infrastructure.Persistence.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserEmail).HasMaxLength(256);
        builder.Property(e => e.Action).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ResourceId).HasMaxLength(200);
        builder.Property(e => e.Details).HasMaxLength(500);

        // Admin ekranı (Gün 2) muhtemelen "en yeni önce" sıralayacak ve/veya
        // Action'a göre filtreleyecek - ikisi de sık kullanılacak sorgu yolları.
        builder.HasIndex(e => e.OccurredAtUtc);
        builder.HasIndex(e => e.Action);
    }
}
