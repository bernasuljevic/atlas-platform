using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Vault.Domain.Entities;

/// <summary>
/// Bir sistem/uygulama/sunucu şifresi kaydı - "Şifreler / Güvenli Bilgiler"
/// özelliğinin çekirdek entity'si (Faz 7, "Atlas İçerik Sistemi" spec'i).
///
/// BİLEREK Wiki'nin WikiPage'inden TAMAMEN AYRI bir entity/tablo - bu, spec'in
/// "şifre kayıtları normal Wiki içeriği gibi davranmamalı, semantic search'e
/// karışmamalı, notification/activity feed'de görünmemeli" isteğini KOD
/// SEVİYESİNDE garanti ediyor: PasswordEntry hiçbir zaman WikiPageCreatedEvent
/// yayınlamıyor, AI'ın embedding pipeline'ına hiç girmiyor - çünkü o pipeline
/// SADECE WikiPage'i dinliyor, PasswordEntry'nin varlığından haberi bile yok.
///
/// EncryptedPassword - DÜZ METİN DEĞİL, PBKDF2 HASH DE DEĞİL (bkz.
/// Pbkdf2PasswordHasher - o TEK YÖNLÜ, giriş şifresi doğrulamak için doğru
/// ama burada YANLIŞ olurdu çünkü Vault'un düz metni GERİ ÇÖZEBİLMESİ
/// gerekiyor). Application katmanındaki IPasswordEncryptor (ASP.NET Core Data
/// Protection API) tarafından üretilen, İKİ YÖNLÜ şifreli bir metin - Domain
/// katmanı ŞİFRELEMENİN NASIL yapıldığını bilmiyor, sadece zaten şifrelenmiş
/// bir string'i saklıyor (Application/Handler şifreleme işini yapıp Domain'e
/// hazır ciphertext'i veriyor - tıpkı PasswordHash'in Auth modülünde Handler
/// tarafından hazırlanıp User.Create'e verilmesi gibi).
/// </summary>
public class PasswordEntry : Entity<Guid>
{
    public string Title { get; private set; } = default!;
    public string? Username { get; private set; }
    public string EncryptedPassword { get; private set; } = default!;
    public string? Url { get; private set; }
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public string? Notes { get; private set; }

    // Wiki'nin CreatedByUserId'siyle AYNI desen - Auth'un User tablosuna
    // referans YOK (modüller arası izolasyon), sadece ID. Vault'ta bunun
    // ayrıca çok kritik bir ikinci rolü var: "kimin görebileceği" yetki
    // kuralının TEK dayanağı bu (bkz. Application katmanındaki Handler'lar -
    // sadece oluşturan kullanıcı + Admin görebiliyor, Wiki'deki departman
    // görünürlüğünün bir karşılığı YOK, Vault'ta departman kavramı yok).
    public Guid CreatedByUserId { get; private set; }

    // WikiPage.CreatedByEmail / AuditLogEntry.UserEmail ile AYNI gerekçe -
    // Gün 2'de eklendi çünkü Admin'in "kimin şifresi bu" sorusuna liste
    // ekranında cevap verebilmesi gerekiyor, Auth'un User tablosuna geri
    // sorgu atmak modül izolasyonunu ihlal ederdi.
    public string? CreatedByEmail { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    // "Access history" (spec'in isteğe bağlı önerdiği bir özellik) için TAM
    // bir log değil, sadece EN SON ne zaman görüntülendiğini tutan tek bir
    // alan - tam erişim geçmişi (kim/ne zaman listesi) zaten Audit modülünün
    // işi (bkz. "PasswordEntry.Revealed" audit kaydı, Gün 2), burada
    // TEKRARLANMIYOR.
    public DateTime? LastAccessedAtUtc { get; private set; }

    private PasswordEntry() { }

    private PasswordEntry(
        Guid id, string title, string? username, string encryptedPassword, string? url,
        string? description, string? category, string? notes, Guid createdByUserId,
        string? createdByEmail, DateTime createdAtUtc)
        : base(id)
    {
        Title = title;
        Username = username;
        EncryptedPassword = encryptedPassword;
        Url = url;
        Description = description;
        Category = category;
        Notes = notes;
        CreatedByUserId = createdByUserId;
        CreatedByEmail = createdByEmail;
        CreatedAtUtc = createdAtUtc;
    }

    public static PasswordEntry Create(
        string title, string? username, string encryptedPassword, string? url,
        string? description, string? category, string? notes, Guid createdByUserId, string? createdByEmail)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Başlık boş olamaz.", nameof(title));

        if (string.IsNullOrWhiteSpace(encryptedPassword))
            throw new ArgumentException("Şifrelenmiş parola boş olamaz.", nameof(encryptedPassword));

        return new PasswordEntry(
            Guid.NewGuid(), title.Trim(), NormalizeOptional(username), encryptedPassword,
            NormalizeOptional(url), NormalizeOptional(description), NormalizeOptional(category),
            NormalizeOptional(notes), createdByUserId, createdByEmail, DateTime.UtcNow);
    }

    // encryptedPassword BİLEREK Application katmanından hazır geliyor - Handler
    // "kullanıcı yeni bir parola girdi mi?" kararını (bkz. UpdatePasswordEntryCommandHandler)
    // burada DEĞİL, kendi içinde veriyor (girmediyse mevcut EncryptedPassword'ü
    // aynen tekrar geçiyor) - Domain sadece "hazır bir ciphertext'i kaydet" işini biliyor.
    public void Update(
        string title, string? username, string encryptedPassword, string? url,
        string? description, string? category, string? notes)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Başlık boş olamaz.", nameof(title));

        if (string.IsNullOrWhiteSpace(encryptedPassword))
            throw new ArgumentException("Şifrelenmiş parola boş olamaz.", nameof(encryptedPassword));

        Title = title.Trim();
        Username = NormalizeOptional(username);
        EncryptedPassword = encryptedPassword;
        Url = NormalizeOptional(url);
        Description = NormalizeOptional(description);
        Category = NormalizeOptional(category);
        Notes = NormalizeOptional(notes);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAccessed()
    {
        LastAccessedAtUtc = DateTime.UtcNow;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
