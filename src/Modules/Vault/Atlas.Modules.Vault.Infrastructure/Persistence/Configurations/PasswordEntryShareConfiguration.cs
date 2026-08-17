using Atlas.Modules.Vault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Modules.Vault.Infrastructure.Persistence.Configurations;

public class PasswordEntryShareConfiguration : IEntityTypeConfiguration<PasswordEntryShare>
{
    public void Configure(EntityTypeBuilder<PasswordEntryShare> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SharedWithEmail).HasMaxLength(256);

        // Aynı kayıt aynı kullanıcıyla İKİ KEZ paylaşılamaz (SharePasswordEntryCommandHandler
        // zaten önden kontrol ediyor, bu index SON çizgi/güvenlik ağı).
        builder.HasIndex(s => new { s.PasswordEntryId, s.SharedWithUserId }).IsUnique();

        // GetPasswordEntriesQueryHandler'ın "benimle paylaşılanlar" sorgusu
        // (GetEntryIdsSharedWithUserAsync) SharedWithUserId'ye göre filtreliyor.
        builder.HasIndex(s => s.SharedWithUserId);
    }
}
