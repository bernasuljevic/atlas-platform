using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence.Configurations;

public class UserPageFavoriteConfiguration : IEntityTypeConfiguration<UserPageFavorite>
{
    public void Configure(EntityTypeBuilder<UserPageFavorite> builder)
    {
        builder.HasKey(f => f.Id);

        // Aynı kullanıcı aynı sayfayı iki kez favoriye ekleyemez - Toggle
        // Handler'ı zaten "var mı" kontrolü yapıyor, bu index veritabanı
        // seviyesinde ikinci bir güvence (yarış durumuna karşı). Ayrıca
        // GetByUserAsync'in ana erişim yolu olduğu için sorgu performansı da
        // sağlıyor.
        builder.HasIndex(f => new { f.UserId, f.WikiPageId }).IsUnique();
    }
}
