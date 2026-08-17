using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Wiki.Domain.Entities;

/// <summary>
/// Documents modülündeki DocumentVersion'ın BİREBİR aynı deseni (P6, bkz. o
/// entity'nin yorumu) - bir sayfa "düzenle" ile GÜNCELLENDİĞİNDE, o ana kadar
/// GÜNCEL olan içeriğin anlık görüntüsü (snapshot) buraya taşınıyor;
/// `WikiPage`'in kendisi HER ZAMAN en güncel versiyonu taşımaya devam ediyor.
/// "Versiyon 3" arıyorsan ya bir `WikiPageVersion` satırısındır (eskiyse) ya
/// `WikiPage`'in kendisidir (güncelse) - bu ayrım kasıtlı, her okuma "güncel
/// sayfayı getir" için ekstra bir sorgu gerektirmesin diye.
///
/// `WikiPage`'e FK İLE BAĞLI DEĞİL - DocumentVersion.DocumentId'yle AYNI
/// gerekçe (bu projede FK'ler sadece Wiki'nin cross-module, ham SQL
/// migration'ındaki istisnai durumda var) - temizlik DB cascade'ine değil
/// Handler'ın orkestrasyonuna bırakılıyor.
///
/// `EditedByUserId`/`EditedByEmail` - DocumentVersion.CreatedByUserId'deki
/// AYNI bilinçli sadeleştirme: bu içeriği İLK YAZAN değil, bu versiyonu
/// DEĞİŞTİREN (yerine yenisini kaydeden) kişi. Orijinal yazar zaten
/// `WikiPage.CreatedByUserId`'de duruyor (versiyon 1 için hep doğru).
/// </summary>
public class WikiPageVersion : Entity<Guid>
{
    public Guid WikiPageId { get; private set; }
    public int VersionNumber { get; private set; }

    public string Title { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public string Visibility { get; private set; } = default!;
    public string? Tags { get; private set; }

    public Guid EditedByUserId { get; private set; }
    public string? EditedByEmail { get; private set; }
    public DateTime EditedAtUtc { get; private set; }

    private WikiPageVersion() { }

    private WikiPageVersion(
        Guid id, Guid wikiPageId, int versionNumber, string title, string content, string visibility,
        string? tags, Guid editedByUserId, string? editedByEmail, DateTime editedAtUtc) : base(id)
    {
        WikiPageId = wikiPageId;
        VersionNumber = versionNumber;
        Title = title;
        Content = content;
        Visibility = visibility;
        Tags = tags;
        EditedByUserId = editedByUserId;
        EditedByEmail = editedByEmail;
        EditedAtUtc = editedAtUtc;
    }

    // "CreateSnapshot" - DocumentVersion'daki AYNI isimlendirme gerekçesi:
    // bu bir kullanıcı eylemiyle birebir eşleşen bir oluşturma değil, var
    // olan bir WikiPage'in o anki hâlinin arşive alınması.
    public static WikiPageVersion CreateSnapshot(
        Guid wikiPageId, int versionNumber, string title, string content, string visibility, string? tags,
        Guid editedByUserId, string? editedByEmail)
    {
        if (wikiPageId == Guid.Empty)
            throw new ArgumentException("WikiPageId boş olamaz.", nameof(wikiPageId));

        if (versionNumber <= 0)
            throw new ArgumentException("VersionNumber pozitif olmalı.", nameof(versionNumber));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Başlık boş olamaz.", nameof(title));

        if (editedByUserId == Guid.Empty)
            throw new ArgumentException("EditedByUserId boş olamaz.", nameof(editedByUserId));

        return new WikiPageVersion(
            Guid.NewGuid(), wikiPageId, versionNumber, title, content, visibility, tags,
            editedByUserId, editedByEmail, DateTime.UtcNow);
    }
}
