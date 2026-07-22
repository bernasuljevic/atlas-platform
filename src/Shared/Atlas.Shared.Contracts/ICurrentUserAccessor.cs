namespace Atlas.Shared.Contracts;

/// <summary>
/// BU DOSYA "MODÜLLER ARASI KAPI" DEDİĞİMİZ ŞEYİN SOMUT ÖRNEĞİ.
///
/// İleride Wiki modülü yazınca şunu soracak: "Bu wiki sayfasını kim oluşturdu?"
/// Bunun cevabını Auth modülü biliyor. Ama Wiki, Auth'un içindeki User entity'sini,
/// veritabanı tablosunu falan GÖRMEMELİ - sadece bu interface üzerinden sorar.
///
/// Akış: Auth.Infrastructure bu interface'i implemente edecek (JWT token'dan kullanıcıyı okuyarak),
/// Host'ta DI'a kaydedilecek, Wiki.Application constructor'ından enjekte edip kullanacak.
/// Wiki, "nasıl" öğrendiğini hiç bilmeyecek.
/// </summary>
public interface ICurrentUserAccessor
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }

    // Kullanıcının token'daki (imzalı, sahtesi yapılamayan) gerçek departmanı.
    // Wiki modülü, "hangi departmanın DepartmentOnly sayfalarını görebilirim"
    // sorusunu artık BUNA göre cevaplıyor - istemcinin gönderdiği bir query
    // parametresine göre değil (bkz. GetWikiPagesQueryHandler).
    string? Department { get; }
}
