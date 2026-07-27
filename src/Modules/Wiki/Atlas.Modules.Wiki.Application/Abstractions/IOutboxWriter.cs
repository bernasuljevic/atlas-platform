using MediatR;

namespace Atlas.Modules.Wiki.Application.Abstractions;

/// <summary>
/// Gün 2'de CreateWikiPageCommandHandler/DeleteWikiPageCommandHandler,
/// IPublisher.Publish(event) yerine bunu kullanacak. DİKKAT: Enqueue "async"
/// DEĞİL ve bilerek kendi SaveChanges'ini çağırmıyor - sadece DbContext'in
/// change tracker'ına bir OutboxMessage ekliyor. Asıl veritabanına yazma,
/// çağıran Handler'ın WikiPage için ZATEN yapacağı SaveChangesAsync ile AYNI
/// anda, AYNI transaction'da gerçekleşecek (atomiklik burada sağlanıyor -
/// WikiPage kaydı başarısız olursa Outbox mesajı da hiç yazılmaz).
/// </summary>
public interface IOutboxWriter
{
    void Enqueue(INotification notification);
}
