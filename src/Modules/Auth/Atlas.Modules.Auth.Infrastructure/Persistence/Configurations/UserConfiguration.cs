using Atlas.Modules.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Auth.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        // Aynı email ile iki kullanıcı olamaz - bunu DB seviyesinde de garanti altına alıyoruz,
        // sadece Application katmanındaki kontrole güvenmiyoruz (race condition'a karşı).
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        // Enum'lar varsayılan olarak zaten int olarak saklanır, ama bunu BİLEREK
        // açıkça yazıyoruz - "neden int" sorusuna gelecekte cevap versin diye.
        // Alternatif olarak .HasConversion<string>() ile "Admin"/"Member" diye
        // okunabilir string de saklanabilirdi - biz basitlik için int'i seçtik.
        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<int>();
    }
}
