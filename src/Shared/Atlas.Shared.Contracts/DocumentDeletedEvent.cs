using MediatR;

namespace Atlas.Shared.Contracts;

/// <summary>
/// WikiPageDeletedEvent'in Documents karşılığı - AI modülü (Gün 5'te) buna
/// abone olup DocumentEmbedding satırlarını temizleyecek. Bugün (Gün 3) hiç
/// abonesi yok - Documents kendi Outbox'ı üzerinden yayınlıyor, kimse
/// dinlemiyor, MediatR bunu sessizce no-op olarak kabul ediyor (Wiki'nin
/// AI/Notifications var olmadan ÖNCE de aynı şekilde çalışmasıyla tutarlı).
/// </summary>
public record DocumentDeletedEvent(Guid DocumentId) : INotification;
