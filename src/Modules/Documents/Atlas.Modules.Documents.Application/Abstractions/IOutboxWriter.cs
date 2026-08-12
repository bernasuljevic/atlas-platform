using MediatR;

namespace Atlas.Modules.Documents.Application.Abstractions;

/// <summary>
/// Wiki'nin IOutboxWriter'ıyla AYNI sözleşme. Enqueue "async" DEĞİL ve BİLEREK
/// kendi SaveChanges'ini çağırmıyor - sadece DbContext'in change tracker'ına
/// bir OutboxMessage ekliyor. Asıl veritabanına yazma, çağıran Handler'ın
/// Document için ZATEN yapacağı SaveChangesAsync (IUnitOfWork üzerinden) ile
/// AYNI anda, AYNI transaction'da gerçekleşecek - atomiklik burada sağlanıyor.
/// </summary>
public interface IOutboxWriter
{
    void Enqueue(INotification notification);
}
