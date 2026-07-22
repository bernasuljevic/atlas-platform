using MediatR;

namespace Atlas.Shared.Contracts;

/// <summary>
/// Wiki modülü yeni bir sayfa oluşturulduğunda bu event'i yayınlar (Publish).
/// Notifications modülü buna abone olup bağlı istemcilere bildirim gönderir.
/// Wiki, Notifications'ın var olduğunu bilmez - bu event Shared.Contracts'ta
/// yaşadığı için her iki modül de ona bağımlı olabilir, birbirine değil.
/// </summary>
public record WikiPageCreatedEvent(Guid PageId, string Title, string DepartmentName) : INotification;