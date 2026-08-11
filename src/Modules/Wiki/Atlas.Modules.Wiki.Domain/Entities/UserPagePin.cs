using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Wiki.Domain.Entities;

/// <summary>
/// UserPageFavorite'in pin karşılığı - bkz. o dosyadaki not. Ayrı bir tablo
/// olmasının gerekçesi: "favori" ve "pin" farklı kullanıcı niyetleri (sonra bul
/// vs. sürekli hızlı eriş), bir sayfa ikisinden sadece birinde ya da ikisinde
/// birden olabilmeli - tek bir tabloya "IsFavorite"/"IsPinned" iki bool kolonu
/// eklemek yerine iki ayrı, birbirinden bağımsız toggle edilebilen tablo daha
/// temiz bir model (silme/ekleme her biri için ayrı, tek satırlık bir işlem).
/// </summary>
public class UserPagePin : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid WikiPageId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private UserPagePin() { }

    private UserPagePin(Guid id, Guid userId, Guid wikiPageId, DateTime createdAtUtc) : base(id)
    {
        UserId = userId;
        WikiPageId = wikiPageId;
        CreatedAtUtc = createdAtUtc;
    }

    public static UserPagePin Create(Guid userId, Guid wikiPageId)
        => new(Guid.NewGuid(), userId, wikiPageId, DateTime.UtcNow);
}
