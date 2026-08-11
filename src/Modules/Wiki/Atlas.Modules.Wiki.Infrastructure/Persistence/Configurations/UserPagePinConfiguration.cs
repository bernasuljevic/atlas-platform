using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence.Configurations;

public class UserPagePinConfiguration : IEntityTypeConfiguration<UserPagePin>
{
    public void Configure(EntityTypeBuilder<UserPagePin> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.UserId, p.WikiPageId }).IsUnique();
    }
}
