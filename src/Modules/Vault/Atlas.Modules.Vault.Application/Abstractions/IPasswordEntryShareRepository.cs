using Atlas.Modules.Vault.Domain.Entities;

namespace Atlas.Modules.Vault.Application.Abstractions;

// IPasswordEntryRepository'deki AYNI "AddAsync/DeleteAsync kendi
// SaveChangesAsync'ini çağırır" basitliği - Vault'ta Wiki'nin Outbox
// deseninin gerektirdiği türden bir atomiklik ihtiyacı yok.
public interface IPasswordEntryShareRepository
{
    // Reveal/GetById yetkilendirmesinde "bu kullanıcıyla paylaşılmış mı"
    // sorusuna DOĞRUDAN cevap - listeyi çekip bellekte aramak yerine.
    Task<bool> IsSharedWithAsync(Guid passwordEntryId, Guid userId, CancellationToken cancellationToken);

    Task<PasswordEntryShare?> GetAsync(Guid passwordEntryId, Guid sharedWithUserId, CancellationToken cancellationToken);

    // Bir kaydın "kiminle paylaşıldı" listesi - SADECE sahibi/Admin'e
    // gösteriliyor (bkz. GetPasswordEntrySharesQueryHandler).
    Task<IReadOnlyList<PasswordEntryShare>> GetSharesForEntryAsync(Guid passwordEntryId, CancellationToken cancellationToken);

    // GetPasswordEntriesQueryHandler'ın "benimle paylaşılanlar da listeme
    // girsin" ihtiyacı için - bir kullanıcıyla paylaşılmış TÜM entry ID'leri.
    Task<IReadOnlyList<Guid>> GetEntryIdsSharedWithUserAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(PasswordEntryShare share, CancellationToken cancellationToken);
    Task RemoveAsync(PasswordEntryShare share, CancellationToken cancellationToken);

    // DeletePasswordEntryCommandHandler'ın, WikiPageVersion temizliğiyle AYNI
    // gerekçeyle, bir kayıt silinince onun TÜM paylaşımlarını da temizlemesi için.
    Task DeleteAllForEntryAsync(Guid passwordEntryId, CancellationToken cancellationToken);
}
