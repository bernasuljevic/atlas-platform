using Atlas.Modules.Audit.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Audit.Infrastructure.Persistence;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // AuthDbContext "auth", WikiDbContext "wiki" şemasını kullanıyor - aynı
        // veritabanı (AtlasPlatform), modüller arası sınır SSMS'te bile görünür
        // kalıyor. Aynı desen burada da geçerli.
        modelBuilder.HasDefaultSchema("audit");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditDbContext).Assembly);
    }
}
