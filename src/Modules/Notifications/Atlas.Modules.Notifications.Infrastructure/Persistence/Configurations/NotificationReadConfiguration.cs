using Atlas.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Notifications.Infrastructure.Persistence.Configurations;

public class NotificationReadConfiguration : IEntityTypeConfiguration<NotificationRead>
{
    public void Configure(EntityTypeBuilder<NotificationRead> builder)
    {
        builder.HasKey(r => r.Id);

        // PasswordEntryShare'deki AYNI "aynı çift iki kez okunamaz" garantisi.
        builder.HasIndex(r => new { r.NotificationEntryId, r.UserId }).IsUnique();
    }
}
