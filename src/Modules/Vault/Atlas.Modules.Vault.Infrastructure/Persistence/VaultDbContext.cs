using Atlas.Modules.Vault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Vault.Infrastructure.Persistence;

public class VaultDbContext : DbContext
{
    public VaultDbContext(DbContextOptions<VaultDbContext> options) : base(options)
    {
    }

    public DbSet<PasswordEntry> PasswordEntries => Set<PasswordEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Auth "auth", Wiki "wiki", Audit "audit" şemasını kullanıyor - AYNI
        // veritabanı (AtlasPlatform), Vault kendi "vault" şemasıyla katılıyor.
        modelBuilder.HasDefaultSchema("vault");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VaultDbContext).Assembly);
    }
}
