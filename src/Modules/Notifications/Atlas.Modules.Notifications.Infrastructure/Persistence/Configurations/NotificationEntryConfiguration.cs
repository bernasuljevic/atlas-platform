using Atlas.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Notifications.Infrastructure.Persistence.Configurations;

public class NotificationEntryConfiguration : IEntityTypeConfiguration<NotificationEntry>
{
    public void Configure(EntityTypeBuilder<NotificationEntry> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(300);
        builder.Property(e => e.DepartmentName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Visibility).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ActorEmail).HasMaxLength(256);

        // "En yeni önce" sorgusu her zaman bu sütuna göre sıralanacak.
        builder.HasIndex(e => e.CreatedAtUtc);

        // TargetUserId'ye göre filtreleme (GetNotificationsQueryHandler'ın
        // "broadcast OR bana hedeflenmiş" sorgusu) sık çalışıyor.
        builder.HasIndex(e => e.TargetUserId);
    }
}
