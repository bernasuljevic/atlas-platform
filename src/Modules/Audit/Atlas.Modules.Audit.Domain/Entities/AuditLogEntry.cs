using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Audit.Domain.Entities;

/// <summary>
/// Tek bir audit satırı - "kim, ne zaman, ne yaptı" sorusuna cevap.
/// UserId/UserEmail BİLEREK denormalize (Wiki'nin WikiPageEmbedding'deki
/// Title/DepartmentName denormalizasyonuyla aynı gerekçe) - Audit modülü
/// Auth'un User tablosuna hiç referans vermiyor (modüller arası izolasyon
/// kuralı), o anki kullanıcı bilgisi ICurrentUserAccessor'dan okunup buraya
/// KOPYALANIYOR. Bu, kullanıcı daha sonra silinse/e-postası değişse bile
/// audit kaydının o ANKİ gerçeği göstermeye devam etmesini de sağlıyor -
/// tam da bir audit log'un yapması gereken şey.
/// </summary>
public class AuditLogEntry : Entity<Guid>
{
    public Guid? UserId { get; private set; }
    public string? UserEmail { get; private set; }

    // Ör. "WikiPage.Created", "WikiPage.Deleted" - modül+eylem şeklinde sabit
    // bir sözleşme, serbest metin değil (bkz. IAuditableCommand.AuditAction).
    public string Action { get; private set; } = default!;

    // Eylemin ilgili olduğu kaynağın ID'si (ör. WikiPageId) - opsiyonel,
    // her eylemde anlamlı bir "kaynak" olmayabilir.
    public string? ResourceId { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    private AuditLogEntry() { }

    private AuditLogEntry(
        Guid id, Guid? userId, string? userEmail, string action, string? resourceId, DateTime occurredAtUtc)
        : base(id)
    {
        UserId = userId;
        UserEmail = userEmail;
        Action = action;
        ResourceId = resourceId;
        OccurredAtUtc = occurredAtUtc;
    }

    public static AuditLogEntry Create(Guid? userId, string? userEmail, string action, string? resourceId)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action boş olamaz.", nameof(action));

        return new AuditLogEntry(Guid.NewGuid(), userId, userEmail, action, resourceId, DateTime.UtcNow);
    }
}
