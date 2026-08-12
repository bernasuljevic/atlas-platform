using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Documents.Domain.Entities;

/// <summary>
/// P6 (versiyonlama) - bir belgenin dosyası "yeni versiyon yükle" ile
/// DEĞİŞTİRİLDİĞİNDE, o ana kadar GÜNCEL olan dosyanın anlık görüntüsü
/// (snapshot) buraya taşınıyor; `Document`'ın kendisi HER ZAMAN en güncel
/// versiyonun bilgisini taşımaya devam ediyor (StorageKey/ContentType/
/// FileExtension/SizeBytes/ContentHash/CurrentVersionNumber - bkz. Document
/// entity). Yani "versiyon 3" arıyorsan ya bir `DocumentVersion` satırısındır
/// (eski bir versiyonsa) ya da `Document`'ın kendisidir (güncel versiyonsa) -
/// bu ayrım kasıtlı: her okuma "güncel belgeyi getir" için ekstra bir JOIN/
/// "en son versiyonu bul" sorgusu gerektirmesin diye.
///
/// `Document` entity'sine FK İLE BAĞLI DEĞİL - WikiPageEmbedding.WikiPageId'yle
/// AYNI gerekçe (bu projede FK'ler sadece Wiki'nin cross-module, ham SQL
/// migration'ındaki istisnai durumda var; aynı modül içinde bile açıkça
/// tercih edilmiyor) - temizlik (silme) DB cascade'ine değil, Handler'ın
/// açıkça orkestre etmesine bırakılıyor (bkz. DeleteDocumentCommandHandler).
///
/// `CreatedByUserId`/`CreatedByEmail`/`CreatedAtUtc` - BİLİNÇLİ BİR SADELEŞTİRME:
/// bu, o içeriği İLK YÜKLEYEN kişi DEĞİL, bu versiyonu DEĞİŞTİREN (yerine
/// yenisini yükleyen) kişi - "bu snapshot ne zaman ve kimin işlemiyle arşive
/// düştü" sorusuna cevap veriyor. Orijinal yükleyici bilgisi zaten
/// `Document.CreatedByUserId`'de duruyor (versiyon 1 için hep doğru), P6'nın
/// hedefi ("eski bir versiyona dönebilmek/indirebilmek") için bu ayrım
/// yeterli - "bu içeriği asıl kim yazdı" sorusu Document Library'nin
/// kapsamı dışında (Vault'un password history tutmamasıyla AYNI YAGNI kararı).
/// </summary>
public class DocumentVersion : Entity<Guid>
{
    public Guid DocumentId { get; private set; }

    public int VersionNumber { get; private set; }

    public string OriginalFileName { get; private set; } = default!;
    public string StorageKey { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public string FileExtension { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public string ContentHash { get; private set; } = default!;

    public Guid CreatedByUserId { get; private set; }
    public string? CreatedByEmail { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private DocumentVersion() { }

    private DocumentVersion(
        Guid id, Guid documentId, int versionNumber, string originalFileName, string storageKey,
        string contentType, string fileExtension, long sizeBytes, string contentHash,
        Guid createdByUserId, string? createdByEmail, DateTime createdAtUtc) : base(id)
    {
        DocumentId = documentId;
        VersionNumber = versionNumber;
        OriginalFileName = originalFileName;
        StorageKey = storageKey;
        ContentType = contentType;
        FileExtension = fileExtension;
        SizeBytes = sizeBytes;
        ContentHash = contentHash;
        CreatedByUserId = createdByUserId;
        CreatedByEmail = createdByEmail;
        CreatedAtUtc = createdAtUtc;
    }

    // "CreateSnapshot" ismi bilerek "Create" değil - bu bir kullanıcı eylemiyle
    // BİREBİR eşleşen bir oluşturma değil (Document.Create'in aksine), var olan
    // bir Document'ın o anki halinin ARŞİVE alınması.
    public static DocumentVersion CreateSnapshot(
        Guid documentId, int versionNumber, string originalFileName, string storageKey,
        string contentType, string fileExtension, long sizeBytes, string contentHash,
        Guid createdByUserId, string? createdByEmail)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId boş olamaz.", nameof(documentId));

        if (versionNumber <= 0)
            throw new ArgumentException("VersionNumber pozitif olmalı.", nameof(versionNumber));

        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Dosya adı boş olamaz.", nameof(originalFileName));

        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key boş olamaz.", nameof(storageKey));

        if (string.IsNullOrWhiteSpace(contentHash))
            throw new ArgumentException("İçerik hash'i boş olamaz.", nameof(contentHash));

        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId boş olamaz.", nameof(createdByUserId));

        return new DocumentVersion(
            Guid.NewGuid(), documentId, versionNumber, originalFileName.Trim(), storageKey,
            contentType.Trim(), fileExtension.Trim().ToLowerInvariant(), sizeBytes, contentHash,
            createdByUserId, createdByEmail, DateTime.UtcNow);
    }
}
