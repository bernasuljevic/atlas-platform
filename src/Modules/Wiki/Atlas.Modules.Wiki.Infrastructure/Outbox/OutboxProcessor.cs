using System.Text.Json;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Atlas.Modules.Wiki.Infrastructure.Outbox;

/// <summary>
/// Outbox Pattern'in Gün 3'ü - Gün 1-2'de yazılan OutboxMessage satırlarını
/// gerçekten "işleyen" taraf bu. Periyodik olarak (her PollInterval'da) henüz
/// işlenmemiş (ProcessedAtUtc == null) satırları okuyup, Payload'ı EventType'a
/// göre deserialize edip GERÇEK IPublisher.Publish'i BURADA tetikliyor.
///
/// ÖNEMLİ: Notifications ve AI'ın WikiPageCreatedEvent/WikiPageDeletedEvent
/// dinleyen handler'ları HİÇ DEĞİŞMEDİ - onlar hâlâ INotificationHandler&lt;T&gt;,
/// event'in nereden geldiğini (eskiden Handler'ın kendisinden, şimdi buradan)
/// bilmiyorlar/umursamıyorlar. Outbox, MediatR'ın publish akışının SADECE
/// NE ZAMAN tetikleneceğini değiştirdi - kimin dinlediğini değil.
///
/// BackgroundService Singleton olarak çalışıyor ama WikiDbContext/IPublisher
/// Scoped - bu yüzden her turda IServiceScopeFactory ile YENİ bir scope açıyoruz
/// (Singleton bir servisin doğrudan Scoped bir bağımlılık enjekte etmesi zaten
/// derleme zamanında/DI doğrulamasında hataya yol açardı).
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
                // Bir turun TAMAMI beklenmedik şekilde patlarsa (örn. DB bağlantısı
                // koptu) - işleyiciyi tamamen durdurmuyoruz, bir sonraki turda
                // tekrar deneyecek.
                _logger.LogError(ex, "Outbox işleyici bir turda beklenmedik bir hatayla karşılaştı.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WikiDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // Gün 4: Attempts < MaxAttempts filtresi - dead letter'a düşmüş bir
        // mesaj artık bu sorguya hiç girmiyor, bir daha denenmiyor (bkz.
        // OutboxMessage.IsDeadLettered'daki not).
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

        // Hepsi için TEK bir SaveChanges - ya işlendi (ProcessedAtUtc dolu)
        // ya başarısız oldu (Attempts/LastError dolu), her iki durumda da
        // bir sonraki turda aynı satırı boşuna tekrar okumayalım diye kalıcı hale geliyor.
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
            _logger.LogError(ex, "OutboxMessage {MessageId} ({EventType}) işlenemedi.", message.Id, message.EventType);
            message.MarkFailed(ex.Message);

            if (message.IsDeadLettered)
            {
                // MaxAttempts'e tam bu denemede ulaştı - bir daha hiç
                // denenmeyecek, ama satır (Attempts/LastError ile) DB'de
                // görünür kalıyor. Ayrı bir uyarı logu, "bu artık sessizce
                // kayboldu değil, birinin bakması gerekiyor" sinyali veriyor.
                _logger.LogWarning(
                    "OutboxMessage {MessageId} ({EventType}) {MaxAttempts} denemeden sonra dead letter'a düştü, bir daha denenmeyecek.",
                    message.Id, message.EventType, OutboxMessage.MaxAttempts);
            }
        }
    }
}
