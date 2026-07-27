using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Wiki.Domain.Entities;

/// <summary>
/// Transactional Outbox Pattern'in kalbi - Gün 3'te bulunan teknik borcun
/// (MediatR'ın IPublisher'ı event'i in-process, kalıcı OLMAYAN şekilde
/// yayınlıyor; process WikiPage kaydedildikten hemen sonra çökerse event
/// sonsuza kadar kaybolur) çözümü.
///
/// Gün 2'de CreateWikiPageCommandHandler/DeleteWikiPageCommandHandler artık
/// IPublisher.Publish(event) çağırmayacak - onun yerine bu satırı, WikiPage'in
/// KENDİSİYLE AYNI SaveChanges çağrısında (aynı transaction'da, atomik olarak)
/// ekleyecek. Gün 3'teki arka plan işleyici, işlenmemiş (ProcessedAtUtc == null)
/// satırları okuyup gerçek MediatR Publish'i O ZAMAN tetikleyecek - böylece
/// Wiki, AI'ın var olduğundan hâlâ habersiz kalıyor (event içeriği burada da
/// sadece EventType/Payload olarak JSON'a serileştirilmiş ham veri).
/// </summary>
public class OutboxMessage : Entity<Guid>
{
    // Event'in .NET tipinin tam adı (AssemblyQualifiedName) - arka plan işleyici
    // Payload'ı JSON'dan geri bu tipe deserialize edip gerçek Publish'i yapacak.
    public string EventType { get; private set; } = default!;

    public string Payload { get; private set; } = default!;

    public DateTime OccurredAtUtc { get; private set; }

    // null = henüz işlenmedi. Arka plan işleyici bu alana bakarak "hangi
    // satırları henüz yayınlamadım" sorusuna cevap verecek.
    public DateTime? ProcessedAtUtc { get; private set; }

    // Kaç kez denendiği + son hatanın ne olduğu - Gün 4'teki retry/görünürlük
    // stratejisi için.
    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

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
