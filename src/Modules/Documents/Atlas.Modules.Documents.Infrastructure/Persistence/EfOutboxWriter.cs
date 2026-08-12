using System.Text.Json;
using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Domain.Entities;
using MediatR;

namespace Atlas.Modules.Documents.Infrastructure.Persistence;

public class EfOutboxWriter : IOutboxWriter
{
    private readonly DocumentsDbContext _context;

    public EfOutboxWriter(DocumentsDbContext context)
    {
        _context = context;
    }

    public void Enqueue(INotification notification)
    {
        // notification.GetType() - INTERFACE değil, ÇALIŞMA ZAMANI tipini
        // kullanıyoruz; aksi halde System.Text.Json sadece INotification
        // arayüzünün (boş) üyelerini yazardı.
        var runtimeType = notification.GetType();
        var payload = JsonSerializer.Serialize(notification, runtimeType);

        var message = OutboxMessage.Create(runtimeType.AssemblyQualifiedName!, payload);

        // SaveChangesAsync BİLEREK çağrılmıyor - bkz. IOutboxWriter'daki not.
        _context.OutboxMessages.Add(message);
    }
}
