using Atlas.Modules.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Auth.Infrastructure.Persistence;

// Auth modülünün KENDİ DbContext'i - sadece bu modülün entity'lerini tanır.
// Wiki modülü bu sınıfın varlığından habersiz, kendi WikiDbContext'ine sahip.
// İkisi de aynı fiziksel veritabanına yazar ama şema (schema) seviyesinde ayrılırlar.
public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Bu modülün tabloları "auth" şemasında yaşasın - SSMS'te açtığında
        // "auth.Users" göreceksin, Wiki'nin tabloları "wiki" şemasında olacak.
        // Aynı veritabanı, ama modüller arası sınır tablo isimlendirmesinde de görünür.
        modelBuilder.HasDefaultSchema("auth");

        // Bu assembly içindeki tüm IEntityTypeConfiguration<T> sınıflarını otomatik uygula
        // (şu an sadece UserConfiguration var, yeni entity eklendikçe elle kayıt gerekmeyecek).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
