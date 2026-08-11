using Atlas.Modules.Vault.Domain.Entities;

namespace Atlas.Modules.Vault.Application.Abstractions;

// AddAsync/UpdateAsync/DeleteAsync KENDİ SaveChangesAsync'lerini çağırıyor
// (Wiki'nin Outbox-sonrası "stage + tek SaveChanges" deseninin AKSİNE) -
// BİLEREK basit tutuldu: Vault'ta atomik yazılması gereken İKİNCİ bir kayıt
// (Wiki'deki OutboxMessage gibi) YOK, tek bir PasswordEntry'nin kendi başına
// tutarlı yazılması yeterli, bir IUnitOfWork/Outbox indirection'ı burada
// gereksiz karmaşıklık olurdu (YAGNI - Wiki'nin deseni "en gelişmiş" desen,
// her modülün onu kopyalaması ZORUNLU değil, ihtiyaca göre seçiliyor).
public interface IPasswordEntryRepository
{
    Task<PasswordEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // ownerUserId BİLEREK nullable - null verilince TÜM kayıtlar dönüyor
    // (SADECE Admin bu şekilde çağırıyor, bkz. GetPasswordEntriesQueryHandler),
    // bir Guid verilince filtre SQL WHERE'de uygulanıyor (bellekte filtrelemek
    // yerine) - normal bir kullanıcı sorgusu, başkalarının şifrelenmiş
    // parolalarını hiç RAM'e çekmeden en baştan hariç tutuyor. Wiki'nin
    // "tüm veriyi çek, bellekte filtrele" deseninin BİLİNÇLİ TERSİ - Vault'un
    // hassasiyeti bunu gerektiriyor.
    Task<IReadOnlyList<PasswordEntry>> GetAllAsync(Guid? ownerUserId, CancellationToken cancellationToken);

    Task AddAsync(PasswordEntry entry, CancellationToken cancellationToken);
    Task UpdateAsync(PasswordEntry entry, CancellationToken cancellationToken);
    Task DeleteAsync(PasswordEntry entry, CancellationToken cancellationToken);
}
