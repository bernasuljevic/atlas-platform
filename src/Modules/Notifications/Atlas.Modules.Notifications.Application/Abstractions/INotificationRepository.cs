using Atlas.Modules.Notifications.Domain.Entities;

namespace Atlas.Modules.Notifications.Application.Abstractions;

public interface INotificationRepository
{
    Task AddAsync(NotificationEntry entry, CancellationToken ct = default);

    // Wiki'nin GetAllWikiPagesRawQuery deseniyle AYNI felsefe - FİLTRESİZ,
    // TÜM kayıtları döndürüyor (görünürlük filtresi Handler'da, bellekte
    // uygulanıyor). Audit log'un aksine (DB seviyesi filtre - o tablo
    // sınırsız büyüyebilir) bildirim geçmişi zaten `take` ile sınırlı küçük
    // bir pencere, bu yüzden Wiki'nin "gözat" modeline daha yakın.
    Task<IReadOnlyList<NotificationEntry>> GetRecentAsync(int take, CancellationToken ct = default);

    // WikiPageDeletedEventHandler icin (2026-08-17'de bulunan bir bug'in
    // duzeltmesi: sayfa silinince bildirim gecmisindeki kayit "yetim" olarak
    // kaliyordu, AI'in embedding temizligiyle AYNI siniftan eksikti) -
    // WikiPageVersion'in "sayfa silinince iliskili satirlari da temizle"
    // deseniyle AYNI.
    Task DeleteAllForResourceAsync(Guid resourceId, CancellationToken ct = default);
}
