using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence.Configurations;

public class WikiPageConfiguration : IEntityTypeConfiguration<WikiPage>
{
    public void Configure(EntityTypeBuilder<WikiPage> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Content)
            .IsRequired();

        builder.Property(p => p.DepartmentName)
            .IsRequired()
            .HasMaxLength(100);

        // Visibility bir enum (Public/DepartmentOnly) - EF Core varsayılan olarak
        // bunu int (0/1) olarak saklar. Şimdilik yeterli; SSMS'te sayı görürsün.
        builder.Property(p => p.Visibility)
            .IsRequired();

        // department + visibility'ye göre filtreleme sık kullanılacağı için index ekliyoruz.
        builder.HasIndex(p => new { p.DepartmentName, p.Visibility });
    }
}
