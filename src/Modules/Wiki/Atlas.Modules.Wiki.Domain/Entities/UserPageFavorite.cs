using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Wiki.Domain.Entities;

/// <summary>
/// Bir kullanıcının bir wiki sayfasını favorilerine eklediğini kaydeden basit bir
/// ilişki tablosu. Daha önce bu TAMAMEN frontend'de (localStorage, bkz.
/// WikiArticlePage.jsx'teki eski toggle) tutuluyordu - cihazlar arası senkron
/// olmuyordu VE erişimi sonradan kaybedilmiş bir sayfa sessizce listede kalmaya
/// devam ediyordu (gerçek bir tutarlılık açığı - GetFavoritePagesQueryHandler
/// artık her seferinde WikiVisibilityRules ile yeniden süzüyor). UserPagePin'in
/// AYNI şekli - favoriler "sonra kolay bulmak istediğim" sayfalar, pinler
/// "sürekli hızlı erişmek istediğim" sayfalar; kullanıcının isteğiyle bilinçli
/// olarak İKİ AYRI tablo (bir sayfa aynı anda hem favori hem pin olabilmeli,
/// ikisi birbirinden bağımsız açılıp kapanabilmeli).
/// </summary>
public class UserPageFavorite : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid WikiPageId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private UserPageFavorite() { }

    private UserPageFavorite(Guid id, Guid userId, Guid wikiPageId, DateTime createdAtUtc) : base(id)
    {
        UserId = userId;
        WikiPageId = wikiPageId;
        CreatedAtUtc = createdAtUtc;
    }

    public static UserPageFavorite Create(Guid userId, Guid wikiPageId)
        => new(Guid.NewGuid(), userId, wikiPageId, DateTime.UtcNow);
}
