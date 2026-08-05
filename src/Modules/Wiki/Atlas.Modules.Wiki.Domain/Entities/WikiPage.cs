using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Wiki.Domain.Entities;

public class WikiPage : Entity<Guid>
{
    public string Title { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public string DepartmentName { get; private set; } = default!;
    public WikiVisibility Visibility { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    // AI modülünün WikiPageEmbedding'inde Title/DepartmentName'i denormalize
    // etmesiyle AYNI gerekçe: Wiki, Auth'un User tablosunu hiç görmüyor
    // (modüller arası izolasyon) - "bu sayfayı kim oluşturdu" sorusunun
    // insan-okunur cevabı (Dashboard'daki kartlarda gösterilecek) bu yüzden
    // oluşturma anında ICurrentUserAccessor'dan alınıp buraya kopyalanıyor.
    // Nullable - klasörleme özelliğinden önceki (bkz. FolderId'deki AYNI not)
    // gibi, bu alan eklenmeden ÖNCE oluşturulmuş sayfalarda null kalıyor.
    public string? CreatedByEmail { get; private set; }

    // Virgülle ayrılmış, normalize edilmiş (trim + küçük harf + tekrarsız)
    // etiketler - ör. "react,frontend,hooks". Ayrı bir Tag entity'si/many-to-many
    // ilişkisi BİLEREK kurulmadı - tek ihtiyaç "bu metni içeriyor mu" bazlı
    // arama (bkz. SearchWikiPageSuggestionsQueryHandler), bunun için tam bir
    // ilişkisel model YAGNI olurdu. Nullable - hiç etiket girilmemişse null.
    public string? Tags { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    // Null = hiç düzenlenmedi, hâlâ ilk haliyle duruyor. Update() çağrılınca dolar.
    public DateTime? UpdatedAtUtc { get; private set; }

    // Nullable - null "bu sayfa herhangi bir klasöre dosyalanmamış, departmanın
    // kök seviyesinde" anlamına gelir. Var olan tüm sayfalar (klasörleme
    // özelliğinden önce oluşturulmuş) migration sonrası otomatik olarak bu
    // duruma düşer - geriye dönük kırılma yok.
    public Guid? FolderId { get; private set; }

    private WikiPage() { }

    private WikiPage(
        Guid id, string title, string content, string departmentName,
        WikiVisibility visibility, Guid createdByUserId, DateTime createdAtUtc,
        Guid? folderId, string? createdByEmail, string? tags) : base(id)
    {
        Title = title;
        Content = content;
        DepartmentName = departmentName;
        Visibility = visibility;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        FolderId = folderId;
        CreatedByEmail = createdByEmail;
        Tags = tags;
    }

    public static WikiPage Create(string title, string content, string departmentName,
        WikiVisibility visibility, Guid createdByUserId, Guid? folderId = null, string? createdByEmail = null,
        string? tags = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Başlık boş olamaz.", nameof(title));

        if (string.IsNullOrWhiteSpace(departmentName))
            throw new ArgumentException("Departman adı boş olamaz.", nameof(departmentName));

        return new WikiPage(
            Guid.NewGuid(), title.Trim(), content, departmentName.Trim(),
            visibility, createdByUserId, DateTime.UtcNow, folderId, createdByEmail, NormalizeTags(tags));
    }

    // "React, Frontend ,react" gibi kullanıcı girdisini "frontend,react"e
    // indirgiyor - trim + küçük harf + tekrarsız + boş parçalar atılıyor.
    // Hiç anlamlı etiket kalmazsa null (boş string DEĞİL) döndürülüyor, tıpkı
    // diğer nullable denormalize alanlar gibi "yok" anlamı taşısın diye.
    private static string? NormalizeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) return null;

        var normalized = tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .ToList();

        return normalized.Count == 0 ? null : string.Join(",", normalized);
    }

    /// <summary>
    /// Yetkilendirme kuralının kalbi - artık gerçek mantık WikiVisibilityRules'ta yaşıyor
    /// (hem bu Domain nesnesi hem de Application katmanındaki DTO aynı kuralı kullanabilsin
    /// diye). Bu metod sadece o paylaşılan kurala ince bir sarmalayıcı (wrapper).
    /// </summary>
    public bool IsVisibleTo(string? viewerDepartmentName, bool viewerIsAdmin = false)
    {
        return WikiVisibilityRules.IsVisibleTo(Visibility, DepartmentName, viewerDepartmentName, viewerIsAdmin);
    }

    // DepartmentName BİLEREK burada değiştirilemez - bkz. UpdateWikiPageCommand'daki not.
    public void Update(string title, string content, WikiVisibility visibility, Guid? folderId, string? tags = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Başlık boş olamaz.", nameof(title));

        Title = title.Trim();
        Content = content;
        Visibility = visibility;
        FolderId = folderId;
        Tags = NormalizeTags(tags);
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
