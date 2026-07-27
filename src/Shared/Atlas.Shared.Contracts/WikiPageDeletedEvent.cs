using MediatR;

namespace Atlas.Shared.Contracts;

/// <summary>
/// WikiPageCreatedEvent'in silme tarafındaki karşılığı - eksikliği bir bug'a
/// yol açtı: DeleteWikiPageCommandHandler bir sayfayı SQL Server'dan silince
/// AI'ın Postgres'teki embedding'lerine hiç haber gitmiyordu, arama silinen
/// bir sayfayı "hayalet" sonuç olarak döndürmeye devam ediyordu (canlı
/// doğrulandı). AI, bu event'e abone olup kendi embedding'lerini temizliyor.
/// </summary>
public record WikiPageDeletedEvent(Guid PageId) : INotification;
