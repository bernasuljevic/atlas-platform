using Atlas.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Notifications.Infrastructure.Persistence;

public class NotificationsDbContext : DbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options)
    {
    }

    public DbSet<NotificationEntry> NotificationEntries => Set<NotificationEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Audit/Vault/Documents ile AYNI desen - ayrı bir veritabanı DEĞİL,
        // aynı AtlasPlatform (SQL Server) veritabanını kendi şemasıyla paylaşıyor.
        modelBuilder.HasDefaultSchema("notifications");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
