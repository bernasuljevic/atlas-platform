using Atlas.Modules.AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.AI.Infrastructure.Persistence;

public class AiDbContext : DbContext
{
    public AiDbContext(DbContextOptions<AiDbContext> options) : base(options)
    {
    }

    public DbSet<WikiPageEmbedding> WikiPageEmbeddings => Set<WikiPageEmbedding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Auth "auth", Wiki "wiki" şemasını kullanıyor - bu da "ai" şemasını kullanıyor.
        // Farklı bir fiziksel veritabanı (PostgreSQL, AtlasAi) olsa da aynı isimlendirme
        // deseni korunuyor.
        modelBuilder.HasDefaultSchema("ai");

        // pgvector imajı "vector" tipini SAĞLIYOR ama veritabanında extension olarak
        // ETKİNLEŞTİRİLMESİ gerekiyor - bu satır olmadan migration "type vector does
        // not exist" hatasıyla patlıyor (bunu çalıştırıp öğrendik).
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AiDbContext).Assembly);
    }
}
