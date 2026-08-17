using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Notifications.Domain.Entities;

/// <summary>
/// Bir bildirimin KALICI kaydı - Notifications modülü şimdiye kadar TAMAMEN
/// ephemeral'dı (sadece SignalR üzerinden anlık broadcast, hiçbir yerde
/// saklanmıyordu - bağlı değilken kaçırılan bir bildirim SONSUZA KADAR
/// kayboluyordu). Bu entity, kullanıcının "diğerlerinin yazdıkları"/bildirim
/// geçmişi isteğiyle eklendi (2026-08-15).
///
/// Title/DepartmentName/Visibility BİLEREK denormalize - WikiPageEmbedding'in
/// AYNI alanları denormalize etmesiyle birebir aynı gerekçe: Notifications,
/// Wiki'nin veritabanına geri sorgu ATMIYOR (modüller arası izolasyon kuralı),
/// event ANINDAKİ bilgiyi kendi tablosuna kopyalıyor.
///
/// GÜVENLİK (kritik): DepartmentName/Visibility'nin burada durmasının asıl
/// sebebi süs değil - bildirim geçmişini listeleyen sorgu, Wiki listesi/AI
/// aramasıyla AYNI IWikiVisibilityChecker kuralını uygulayabilsin diye
/// buradalar. Bu alanlar olmadan, DepartmentOnly bir sayfanın oluşturulduğu
/// bilgisi (başlığıyla birlikte) o departmanda OLMAYAN kullanıcılara da
/// sızardı - Ders #10'daki (departman görünürlük açığı) sınıftan bir hata.
///
/// TargetUserId (2026-08-17, "gerçek bildirim sistemi" isteği) - BİLEREK
/// nullable: null ise (WikiPageCreated'daki gibi) BROADCAST bir bildirim,
/// departman/görünürlük filtresini geçen HERKES görebiliyor - eski davranış
/// HİÇ DEĞİŞMEDİ. Dolu ise (DiscussionReply gibi) SADECE o kullanıcıya
/// gösteriliyor - bir tartışmaya cevap geldiğinde bunu SADECE ilgili
/// kişilerin (sayfa sahibi + önceki yorumcular) görmesi gerekiyor, departman
/// bazlı bir "herkese açık" bildirim YANLIŞ olurdu. Departman/görünürlük
/// filtresi TargetUserId dolu bir kayıtta da (savunma amaçlı, ikinci bir
/// katman olarak) UYGULANMAYA devam ediyor.
/// </summary>
public class NotificationEntry : Entity<Guid>
{
    // Şimdilik "WikiPageCreated"/"DiscussionReply" (serbest metin, yeni tipler
    // migration gerektirmeden eklenebilir).
    public string EventType { get; private set; } = default!;
    public Guid ResourceId { get; private set; }
    public string Title { get; private set; } = default!;
    public string DepartmentName { get; private set; } = default!;
    public string Visibility { get; private set; } = default!;
    public string? ActorEmail { get; private set; }
    public Guid? TargetUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private NotificationEntry() { }

    private NotificationEntry(
        Guid id, string eventType, Guid resourceId, string title, string departmentName, string visibility,
        string? actorEmail, Guid? targetUserId, DateTime createdAtUtc)
        : base(id)
    {
        EventType = eventType;
        ResourceId = resourceId;
        Title = title;
        DepartmentName = departmentName;
        Visibility = visibility;
        ActorEmail = actorEmail;
        TargetUserId = targetUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public static NotificationEntry Create(
        string eventType, Guid resourceId, string title, string departmentName, string visibility,
        string? actorEmail, Guid? targetUserId = null)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType boş olamaz.", nameof(eventType));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title boş olamaz.", nameof(title));

        return new NotificationEntry(
            Guid.NewGuid(), eventType, resourceId, title, departmentName, visibility, actorEmail, targetUserId,
            DateTime.UtcNow);
    }
}
