using System.Text.Json;
using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using MediatR;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence;

public class EfOutboxWriter : IOutboxWriter
{
    private readonly WikiDbContext _context;

    public EfOutboxWriter(WikiDbContext context)
    {
        _context = context;
    }

    public void Enqueue(INotification notification)
    {
        // notification.GetType() - INTERFACE değil, ÇALIŞMA ZAMANI tipini
        // (örn. WikiPageCreatedEvent) kullanıyoruz; aksi halde System.Text.Json
        // sadece INotification arayüzünün (boş) üyelerini yazardı.
        var runtimeType = notification.GetType();
        var payload = JsonSerializer.Serialize(notification, runtimeType);

        var message = OutboxMessage.Create(runtimeType.AssemblyQualifiedName!, payload);

        // SaveChangesAsync BİLEREK çağrılmıyor - bkz. IOutboxWriter'daki not.
        _context.OutboxMessages.Add(message);
    }
}
