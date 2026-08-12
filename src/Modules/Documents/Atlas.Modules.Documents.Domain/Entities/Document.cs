using Atlas.Modules.Documents.Domain.Enums;
using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Documents.Domain.Entities;

/// <summary>
/// Wiki sayfasına FK İLE BAĞLI DEĞİL - Vault'un PasswordEntry'si gibi tamamen
/// bağımsız bir entity (bkz. proje mimari kararı: "Document Library" bağımsız
/// gezilebilir bir alan, ama içerik içinden `document:GUID` şeklinde serbest
/// referansla da gömülebilir - tıpkı WikiPage'in `wiki:GUID` ile başka bir
/// WikiPage'e referans vermesi gibi, FK'siz).
///
/// <para><b>StorageKey KULLANICI GİRDİSİNDEN ASLA TÜRETİLMİYOR</b> - path
/// traversal saldırısını bir "sanitize et" kontrolüyle DEĞİL, yapısal olarak
/// imkânsız kılıyoruz: IFileStorageService.SaveAsync() kendi GUID tabanlı
/// anahtarını üretir, orijinal dosya adı (OriginalFileName) SADECE metadata
/// olarak saklanır, hiçbir zaman disk yoluna karışmaz.</para>
///
/// <para><b>DepartmentName/Visibility, WikiPage'inkiyle AYNI semantik</b> -
/// yetkilendirme için yeni bir kural İCAT EDİLMEDİ, Shared.Contracts'taki
/// (zaten genel amaçlı tasarlanmış) IWikiVisibilityChecker doğrudan tekrar
/// kullanılacak (bkz. Gün 4 - liste/detay Query'leri).</para>
///
/// <para><b>Status/ProcessingError/CurrentVersionNumber/ContentHash şimdiden
/// (P3 Gün 1) buradalar</b> ama P4 (processing pipeline) ve P6 (versiyonlama)
/// gelene kadar davranışa bağlanmayacaklar - amaç, o fazlar geldiğinde AYRI
/// bir "kolon ekle" migration'ı yazmak yerine, baştan açılmış kolonları
/// kademeli olarak davranışa bağlamak (var olan satırlara sonradan default
/// değer basma riskinden kaçınmak için).</para>
/// </summary>
public class Document : Entity<Guid>
{
    public string Title { get; private set; } = default!;
    public string OriginalFileName { get; private set; } = default!;
    public string StorageKey { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public string FileExtension { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public string DepartmentName { get; private set; } = default!;
    public DocumentVisibility Visibility { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string? CreatedByEmail { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public string? Description { get; private set; }
    public string? Tags { get; private set; }
    public DocumentStatus Status { get; private set; }
    public string? ProcessingError { get; private set; }
    public int CurrentVersionNumber { get; private set; }
    public string ContentHash { get; private set; } = default!;

    private Document() { }

    private Document(
        Guid id, string title, string originalFileName, string storageKey, string contentType,
        string fileExtension, long sizeBytes, string departmentName, DocumentVisibility visibility,
        Guid createdByUserId, string? createdByEmail, string? description, string? tags,
        string contentHash, DateTime createdAtUtc) : base(id)
    {
        Title = title;
        OriginalFileName = originalFileName;
        StorageKey = storageKey;
        ContentType = contentType;
        FileExtension = fileExtension;
        SizeBytes = sizeBytes;
        DepartmentName = departmentName;
        Visibility = visibility;
        CreatedByUserId = createdByUserId;
        CreatedByEmail = createdByEmail;
        Description = description;
        Tags = tags;
        Status = DocumentStatus.Uploaded;
        CurrentVersionNumber = 1;
        ContentHash = contentHash;
        CreatedAtUtc = createdAtUtc;
    }

    public static Document Create(
        string title, string originalFileName, string storageKey, string contentType, string fileExtension,
        long sizeBytes, string departmentName, DocumentVisibility visibility, Guid createdByUserId,
        string? createdByEmail, string? description, string? tags, string contentHash)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Başlık boş olamaz.", nameof(title));

        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Dosya adı boş olamaz.", nameof(originalFileName));

        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key boş olamaz.", nameof(storageKey));

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("İçerik tipi boş olamaz.", nameof(contentType));

        if (string.IsNullOrWhiteSpace(fileExtension))
            throw new ArgumentException("Dosya uzantısı boş olamaz.", nameof(fileExtension));

        if (sizeBytes <= 0)
            throw new ArgumentException("Dosya boyutu sıfırdan büyük olmalı.", nameof(sizeBytes));

        if (string.IsNullOrWhiteSpace(departmentName))
            throw new ArgumentException("Departman adı boş olamaz.", nameof(departmentName));

        if (string.IsNullOrWhiteSpace(contentHash))
            throw new ArgumentException("İçerik hash'i boş olamaz.", nameof(contentHash));

        return new Document(
            Guid.NewGuid(), title.Trim(), originalFileName.Trim(), storageKey, contentType.Trim(),
            fileExtension.Trim().ToLowerInvariant(), sizeBytes, departmentName.Trim(), visibility,
            createdByUserId, createdByEmail, NormalizeOptional(description), NormalizeOptional(tags),
            contentHash, DateTime.UtcNow);
    }

    // WikiPage.Update'teki AYNI kural: DepartmentName burada da YOK - bir
    // belgenin departmanı oluşturulduktan sonra değiştirilemiyor (departman
    // değişikliği aslında "başka bir yere taşıma", ayrı bir işlem olurdu).
    public void UpdateMetadata(string title, string? description, DocumentVisibility visibility, string? tags)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Başlık boş olamaz.", nameof(title));

        Title = title.Trim();
        Description = NormalizeOptional(description);
        Visibility = visibility;
        Tags = NormalizeOptional(tags);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    // P4'te DocumentUploadedEventHandler tarafından çağrılacak - bugün (P3
    // Gün 1) sadece durum makinesinin Domain tarafı hazır, kimse çağırmıyor.
    // P6 (versiyonlama) - "yeni versiyon yükle" ile çağrılıyor. Handler'ın
    // BU METOTTAN ÖNCE mevcut (şu ana kadar güncel olan) dosya bilgisini bir
    // DocumentVersion'a snapshot'ladığını varsayıyor - burası sadece Document'ı
    // YENİ dosyaya işaret edecek şekilde güncelliyor, eskiyi hiç bilmiyor
    // (tek sorumluluk: "ben artık buyum", arşivleme Handler'ın işi).
    // Status BİLEREK Uploaded'a dönüyor - içerik değişti, eski extraction/
    // embedding artık geçersiz; DocumentUploadedEventHandler (P4) bunu normal
    // bir ilk-yükleme gibi işleyip yeniden Extracting/Ready/Failed'e taşıyacak.
    public void ReplaceFile(
        string originalFileName, string storageKey, string contentType, string fileExtension,
        long sizeBytes, string contentHash)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Dosya adı boş olamaz.", nameof(originalFileName));

        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key boş olamaz.", nameof(storageKey));

        if (sizeBytes <= 0)
            throw new ArgumentException("Dosya boyutu sıfırdan büyük olmalı.", nameof(sizeBytes));

        if (string.IsNullOrWhiteSpace(contentHash))
            throw new ArgumentException("İçerik hash'i boş olamaz.", nameof(contentHash));

        OriginalFileName = originalFileName.Trim();
        StorageKey = storageKey;
        ContentType = contentType.Trim();
        FileExtension = fileExtension.Trim().ToLowerInvariant();
        SizeBytes = sizeBytes;
        ContentHash = contentHash;
        CurrentVersionNumber += 1;
        Status = DocumentStatus.Uploaded;
        ProcessingError = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkExtracting() => Status = DocumentStatus.Extracting;

    public void MarkReady()
    {
        Status = DocumentStatus.Ready;
        ProcessingError = null;
    }

    public void MarkFailed(string error)
    {
        Status = DocumentStatus.Failed;
        ProcessingError = error;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
