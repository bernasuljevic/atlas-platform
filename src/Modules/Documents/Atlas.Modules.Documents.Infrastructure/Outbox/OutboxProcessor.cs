using System.Text.Json;
using Atlas.Modules.Documents.Domain.Entities;
using Atlas.Modules.Documents.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Atlas.Modules.Documents.Infrastructure.Outbox;

/// <summary>
/// Wiki'nin OutboxProcessor'ının BİREBİR kopyası - aynı poll aralığı (5sn),
/// aynı dead-letter eşiği (5 deneme), aynı "her turda yeni scope aç" deseni
/// (BackgroundService Singleton, DocumentsDbContext/IPublisher Scoped).
///
/// P4 Gün 2 itibarıyla bu servis ÇALIŞIYOR ama kuyruğu HENÜZ BOŞ - Documents
/// modülünde onu besleyecek (Enqueue çağıran) hiçbir Command yok, çünkü ilk
/// gerçek event (DocumentUploadedEvent) Gün 3'te tanımlanacak. Bugünün amacı
/// "arka plan işleyici doğru şekilde ayağa kalkıyor ve hatasız dönüyor mu"
/// sorusuna cevap vermek - Wiki'nin AI/Notifications'a bağlanmadan ÖNCE
/// kendi Outbox'ını aynı şekilde önce "boş çalışır" halde doğrulamasıyla
/// tutarlı bir ara adım.
/// </summary>
public class OutboxProcessor : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Documents Outbox işleyici bir turda beklenmedik bir hatayla karşılaştı.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var pendingMessages = await context.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null && m.Attempts < OutboxMessage.MaxAttempts)
            .OrderBy(m => m.OccurredAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
            return;

        foreach (var message in pendingMessages)
        {
            await ProcessMessageAsync(message, publisher, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(OutboxMessage message, IPublisher publisher, CancellationToken cancellationToken)
    {
        try
        {
            var eventType = Type.GetType(message.EventType)
                ?? throw new InvalidOperationException($"'{message.EventType}' tipi çözülemedi.");

            var notification = (INotification?)JsonSerializer.Deserialize(message.Payload, eventType)
                ?? throw new InvalidOperationException("Payload deserialize edilemedi.");

            await publisher.Publish(notification, cancellationToken);

            message.MarkProcessed();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Documents OutboxMessage {MessageId} ({EventType}) işlenemedi.", message.Id, message.EventType);
            message.MarkFailed(ex.Message);

            if (message.IsDeadLettered)
            {
                _logger.LogWarning(
                    "Documents OutboxMessage {MessageId} ({EventType}) {MaxAttempts} denemeden sonra dead letter'a düştü, bir daha denenmeyecek.",
                    message.Id, message.EventType, OutboxMessage.MaxAttempts);
            }
        }
    }
}
