using Atlas.Modules.Documents.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Documents.Infrastructure.Persistence;

public class DocumentsDbContext : DbContext
{
    public DocumentsDbContext(DbContextOptions<DocumentsDbContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Auth "auth", Wiki "wiki", Audit "audit", Vault "vault" şemasını
        // kullanıyor - AYNI veritabanı (AtlasPlatform), Documents kendi
        // "documents" şemasıyla katılıyor.
        modelBuilder.HasDefaultSchema("documents");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentsDbContext).Assembly);
    }
}
