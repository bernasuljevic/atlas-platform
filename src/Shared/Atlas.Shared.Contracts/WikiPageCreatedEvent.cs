using MediatR;

namespace Atlas.Shared.Contracts;

/// <summary>
/// Wiki modülü yeni bir sayfa oluşturulduğunda bu event'i yayınlar (Publish).
/// Notifications modülü buna abone olup bağlı istemcilere bildirim gönderir,
/// AI modülü buna abone olup embedding üretimini tetikler. Wiki, bunları
/// dinleyen kim var bilmez - bu event Shared.Contracts'ta yaşadığı için her
/// modül ona bağımlı olabilir, birbirine değil.
///
/// "Content" alanı AI modülü için eklendi: embedding üretmek için sayfanın asıl
/// metnine ihtiyaç var, event bunu içermeseydi AI modülünün Wiki'nin veritabanına
/// geri dönüp içeriği SORGULAMASI gerekirdi - bu da modüller arası izolasyon
/// kuralını (Wiki'nin DB'sine başka bir modülün doğrudan erişmesi) ihlal ederdi.
/// Notifications bu alanı kullanmıyor ama görmezden gelebiliyor, zararı yok.
///
/// "CreatedByEmail" - Notifications'ın kalıcı bildirim geçmişi özelliği için
/// eklendi (2026-08-15, "Content" alanının eklenme gerekçesiyle AYNI desen:
/// yeni bir tüketicinin ihtiyacı, event'in kendisine eklendi, Wiki'nin DB'sine
/// geri sorgu atmak yerine).
///
/// "IsReindexReplay" - `ReindexWikiPagesCommandHandler` var olan TÜM sayfalar
/// için bu event'i YENİDEN yayınlıyor (embedding sağlayıcısı değişince AI'ın
/// yeniden işlemesi için) - AI'ın handler'ı için bu sorun değil (embedding'i
/// zaten idempotent şekilde yeniden üretiyor), ama Notifications'ın YENİ
/// eklenen kalıcı geçmişi için BÜYÜK bir sorun olurdu: bir reindex çalıştırmak,
/// haftalar/aylar önce oluşturulmuş her sayfa için "az önce oluşturuldu" gibi
/// SAHTE bildirim kayıtları ekleyip geçmişi anlamsızlaştırırdı. Bu bayrak
/// Notifications'ın handler'ına "bu gerçek bir oluşturma değil, sadece AI'ın
/// ilgilenmesi gereken bir yeniden-yayın" demesini sağlıyor - kalıcı kayıt
/// ATLANIYOR (SignalR toast'ı da atlanıyor, reindex sırasında onlarca toast
/// patlaması istenmeyen bir yan etki olurdu).
/// </summary>
public record WikiPageCreatedEvent(
    Guid PageId, string Title, string DepartmentName, string Content, string Visibility,
    string? CreatedByEmail, bool IsReindexReplay = false) : INotification;