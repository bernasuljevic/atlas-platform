using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.AuthorEmail)
            .HasMaxLength(256);

        // PageId null OLABİLİR (genel platform tartışması) - yine de bir
        // sayfanın yorumlarını çekmek sık kullanılacağı için index faydalı.
        builder.HasIndex(c => c.PageId);
    }
}
