using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence;

public class WikiDbContext : DbContext
{
    public WikiDbContext(DbContextOptions<WikiDbContext> options) : base(options)
    {
    }

    public DbSet<WikiPage> WikiPages => Set<WikiPage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // AuthDbContext "auth" şemasını kullanıyor, bu da "wiki" şemasını kullanıyor -
        // aynı veritabanı, modüller arası sınır SSMS'te bile görünür kalıyor.
        modelBuilder.HasDefaultSchema("wiki");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WikiDbContext).Assembly);
    }
}
