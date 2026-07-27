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
/// </summary>
public record WikiPageCreatedEvent(Guid PageId, string Title, string DepartmentName, string Content) : INotification;