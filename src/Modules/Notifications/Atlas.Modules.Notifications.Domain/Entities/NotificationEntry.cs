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
/// </summary>
public class NotificationEntry : Entity<Guid>
{
    // Şimdilik tek bir değer ("WikiPageCreated") ama serbest metin - ileride
    // Documents/başka event tipleri eklenirse (bilerek bu fazın kapsamı
    // dışında bırakıldı) yeni bir migration gerektirmeden genişleyebilsin.
    public string EventType { get; private set; } = default!;
    public Guid ResourceId { get; private set; }
    public string Title { get; private set; } = default!;
    public string DepartmentName { get; private set; } = default!;
    public string Visibility { get; private set; } = default!;
    public string? ActorEmail { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private NotificationEntry() { }

    private NotificationEntry(
        Guid id, string eventType, Guid resourceId, string title, string departmentName, string visibility,
        string? actorEmail, DateTime createdAtUtc)
        : base(id)
    {
        EventType = eventType;
        ResourceId = resourceId;
        Title = title;
        DepartmentName = departmentName;
        Visibility = visibility;
        ActorEmail = actorEmail;
        CreatedAtUtc = createdAtUtc;
    }

    public static NotificationEntry Create(
        string eventType, Guid resourceId, string title, string departmentName, string visibility, string? actorEmail)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType boş olamaz.", nameof(eventType));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title boş olamaz.", nameof(title));

        return new NotificationEntry(
            Guid.NewGuid(), eventType, resourceId, title, departmentName, visibility, actorEmail, DateTime.UtcNow);
    }
}
