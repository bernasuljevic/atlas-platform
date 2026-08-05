using Atlas.Modules.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Auth.Infrastructure.Persistence.Configurations;

public class EmailVerificationCodeConfiguration : IEntityTypeConfiguration<EmailVerificationCode>
{
    public void Configure(EntityTypeBuilder<EmailVerificationCode> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(6);

        // Bir kullanıcının aktif kodunu ("kullanılmamış olanlar arasında en
        // yeni") hızlı bulmak için - hem doğrulama hem "yeniden gönder" bu
        // sorguyu kullanıyor (bkz. EfEmailVerificationCodeRepository).
        builder.HasIndex(c => new { c.UserId, c.UsedAtUtc });
    }
}
