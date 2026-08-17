using Atlas.Modules.Notifications.Domain.Entities;

namespace Atlas.Modules.Notifications.Application.Abstractions;

public interface INotificationRepository
{
    Task AddAsync(NotificationEntry entry, CancellationToken ct = default);

    // Wiki'nin GetAllWikiPagesRawQuery deseniyle AYNI felsefe - departman/
    // görünürlük filtresi Handler'da, bellekte uygulanıyor (audit log'un
    // aksine - DB seviyesi filtre - o tablo sınırsız büyüyebilir, bildirim
    // geçmişi zaten `take` ile sınırlı küçük bir pencere).
    //
    // viewerUserId (2026-08-17, "gerçek bildirim sistemi" isteği) - SADECE
    // TargetUserId filtresini DB seviyesinde uyguluyor (broadcast/TargetUserId
    // null OLAN + BENİM için hedeflenmiş olanlar) - departman/görünürlük
    // filtresi hâlâ Handler'da. viewerUserId null ise (giriş yapmamış/edge
    // case) SADECE broadcast kayıtlar dönüyor.
    Task<IReadOnlyList<NotificationEntry>> GetRecentAsync(Guid? viewerUserId, int take, CancellationToken ct = default);

    // WikiPageDeletedEventHandler icin (2026-08-17'de bulunan bir bug'in
    // duzeltmesi: sayfa silinince bildirim gecmisindeki kayit "yetim" olarak
    // kaliyordu, AI'in embedding temizligiyle AYNI siniftan eksikti) -
    // WikiPageVersion'in "sayfa silinince iliskili satirlari da temizle"
    // deseniyle AYNI.
    Task DeleteAllForResourceAsync(Guid resourceId, CancellationToken ct = default);

    // Okuma durumu (2026-08-17) - PasswordEntryShareRepository'deki AYNI
    // "composite anahtar" deseni. GetReadNotificationIdsAsync, Handler'ın
    // "bu ID'lerden hangileri BU kullanıcı tarafından zaten okunmuş"
    // sorusuna cevap veriyor - tek tek IsRead sorgulamak yerine TOPLU.
    Task<IReadOnlySet<Guid>> GetReadNotificationIdsAsync(
        Guid userId, IReadOnlyCollection<Guid> notificationIds, CancellationToken ct = default);

    // İdempotent - zaten okunmuş bir bildirimi tekrar işaretlemek hata
    // vermiyor, sessizce no-op (kullanıcı aynı bildirime iki kez tıklarsa
    // ikinci tıklama bir hataya YOL AÇMAMALI).
    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);

    // notificationIds: Handler'ın ZATEN görünürlük filtresinden geçirdiği
    // "bu kullanıcının şu an görebildiği" kümesi - repository burada
    // görünürlük kuralını TEKRAR uygulamıyor (o mantık SADECE Application
    // katmanında, IWikiVisibilityChecker üzerinden yaşıyor).
    Task MarkAllAsReadAsync(Guid userId, IReadOnlyCollection<Guid> notificationIds, CancellationToken ct = default);
}
