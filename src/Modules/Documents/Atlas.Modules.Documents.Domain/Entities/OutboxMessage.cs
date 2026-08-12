using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Documents.Domain.Entities;

/// <summary>
/// Wiki.Domain'deki OutboxMessage'ın BİREBİR kopyası (Transactional Outbox
/// Pattern - Wiki'nin geçmişini tekrarlıyoruz, bkz. P4 Gün 2 notu). Documents'ın
/// P3'te Outbox'ı YOKTU (Upload/Delete düz SaveChangesAsync kullanıyordu) -
/// P4'te belge işleme (extraction/embedding) event-tabanlı hale gelince
/// atomiklik gerekiyor: bir belge yüklendiğinde, "işlenmesi gerekiyor" sinyali
/// belgenin KENDİSİYLE AYNI transaction'da kalıcı hale gelmeli, aksi halde
/// process belge kaydedildikten hemen sonra çökerse o belge asla işlenmeden
/// kalırdı.
/// </summary>
public class OutboxMessage : Entity<Guid>
{
    public const int MaxAttempts = 5;

    public string EventType { get; private set; } = default!;

    public string Payload { get; private set; } = default!;

    public DateTime OccurredAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

    public bool IsDeadLettered => ProcessedAtUtc is null && Attempts >= MaxAttempts;

    private OutboxMessage() { }

    private OutboxMessage(Guid id, string eventType, string payload, DateTime occurredAtUtc) : base(id)
    {
        EventType = eventType;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
    }

    public static OutboxMessage Create(string eventType, string payload)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType boş olamaz.", nameof(eventType));

        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload boş olamaz.", nameof(payload));

        return new OutboxMessage(Guid.NewGuid(), eventType, payload, DateTime.UtcNow);
    }

    public void MarkProcessed()
    {
        ProcessedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Attempts++;
        LastError = error;
    }
}
