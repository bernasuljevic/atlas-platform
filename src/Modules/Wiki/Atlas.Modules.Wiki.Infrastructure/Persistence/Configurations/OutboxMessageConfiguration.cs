using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.EventType)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.Payload)
            .IsRequired();

        builder.Property(m => m.LastError)
            .HasMaxLength(2000);

        // Arka plan işleyicinin ana sorgusu "ProcessedAtUtc IS NULL" olacak -
        // bu index olmadan tablo büyüdükçe (işlenmiş satırlar birikince) bu
        // sorgu yavaşlardı.
        builder.HasIndex(m => m.ProcessedAtUtc);
    }
}
