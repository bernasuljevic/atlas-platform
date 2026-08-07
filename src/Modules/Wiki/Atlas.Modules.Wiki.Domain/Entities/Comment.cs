using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Wiki.Domain.Entities;

/// <summary>
/// Wikipedia'nın "Tartışma" sayfası fikri - bir wiki sayfasına (PageId dolu) YA
/// DA platformun genelinde (PageId null - ana sayfadaki "Anasayfa Tartışması")
/// bırakılan düz (thread'siz) bir yorum. Thread'li (yanıtın-yanıtı) yapı
/// BİLEREK kurulmadı - YAGNI, kronolojik düz bir liste ihtiyacın büyük
/// çoğunluğunu karşılıyor, iç içe yanıtlar ayrı bir karmaşıklık katmanı.
/// AuthorEmail diğer denormalize alanlarla (WikiPage.CreatedByEmail vb.) AYNI
/// gerekçeyle kopyalanıyor - yazan kullanıcı silinse/değişse bile yorum kimin
/// olduğunu göstermeye devam etsin.
/// </summary>
public class Comment : Entity<Guid>
{
    public Guid? PageId { get; private set; }
    public string Content { get; private set; } = default!;
    public Guid AuthorUserId { get; private set; }
    public string? AuthorEmail { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Comment() { }

    private Comment(
        Guid id, Guid? pageId, string content, Guid authorUserId,
        string? authorEmail, DateTime createdAtUtc) : base(id)
    {
        PageId = pageId;
        Content = content;
        AuthorUserId = authorUserId;
        AuthorEmail = authorEmail;
        CreatedAtUtc = createdAtUtc;
    }

    public static Comment Create(Guid? pageId, string content, Guid authorUserId, string? authorEmail)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Yorum boş olamaz.", nameof(content));

        return new Comment(Guid.NewGuid(), pageId, content.Trim(), authorUserId, authorEmail, DateTime.UtcNow);
    }
}
