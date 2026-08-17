using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Vault.Domain.Entities;

/// <summary>
/// Vault paylaşım modeli (D grubu, Gün 1, 2026-08-17) - bir PasswordEntry'nin
/// sahibinin (ya da Admin'in) BELİRLİ bir kullanıcıya erişim verdiğini temsil
/// ediyor. WikiPageVersion/DocumentVersion'daki AYNI desen: `PasswordEntry`'ye
/// FK İLE BAĞLI DEĞİL (bu projede FK'ler sadece Wiki'nin cross-module,
/// ham-SQL migration'ındaki istisnai durumda var) - temizlik DB cascade'ine
/// değil Handler'ın orkestrasyonuna bırakılıyor (bkz.
/// DeletePasswordEntryCommandHandler'ın artık şimdi temizlediği not).
///
/// BİLİNÇLİ SINIRLAMA: departman bazlı DEĞİL, SADECE kullanıcı bazlı paylaşım
/// (bkz. PasswordEntry.cs'teki "Vault'ta departman kavramı yok" notu - bu
/// kararla ÇELİŞMEMEK için departman paylaşımı EKLENMEDİ). Bir paylaşım
/// SADECE görüntüleme+reveal yetkisi veriyor - düzenleme/silme HÂLÂ
/// SADECE owner-or-Admin (bkz. Update/DeletePasswordEntryCommandHandler,
/// bu iki Handler'a HİÇ dokunulmadı).
/// </summary>
public class PasswordEntryShare : Entity<Guid>
{
    public Guid PasswordEntryId { get; private set; }
    public Guid SharedWithUserId { get; private set; }

    // PasswordEntry.CreatedByEmail ile AYNI gerekçe - Auth'un User tablosuna
    // geri sorgu atmadan (modül izolasyonu) "kiminle paylaşıldığını" liste
    // ekranında gösterebilmek için denormalize.
    public string? SharedWithEmail { get; private set; }

    public Guid SharedByUserId { get; private set; }
    public DateTime SharedAtUtc { get; private set; }

    private PasswordEntryShare() { }

    private PasswordEntryShare(
        Guid id, Guid passwordEntryId, Guid sharedWithUserId, string? sharedWithEmail,
        Guid sharedByUserId, DateTime sharedAtUtc) : base(id)
    {
        PasswordEntryId = passwordEntryId;
        SharedWithUserId = sharedWithUserId;
        SharedWithEmail = sharedWithEmail;
        SharedByUserId = sharedByUserId;
        SharedAtUtc = sharedAtUtc;
    }

    public static PasswordEntryShare Create(
        Guid passwordEntryId, Guid sharedWithUserId, string? sharedWithEmail, Guid sharedByUserId)
    {
        if (passwordEntryId == Guid.Empty)
            throw new ArgumentException("PasswordEntryId boş olamaz.", nameof(passwordEntryId));

        if (sharedWithUserId == Guid.Empty)
            throw new ArgumentException("SharedWithUserId boş olamaz.", nameof(sharedWithUserId));

        if (sharedWithUserId == sharedByUserId)
            throw new ArgumentException("Bir kayıt kendi sahibiyle paylaşılamaz.", nameof(sharedWithUserId));

        return new PasswordEntryShare(
            Guid.NewGuid(), passwordEntryId, sharedWithUserId, sharedWithEmail, sharedByUserId, DateTime.UtcNow);
    }
}
