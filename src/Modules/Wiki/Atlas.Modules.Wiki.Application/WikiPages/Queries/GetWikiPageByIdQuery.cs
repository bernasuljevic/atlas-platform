using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

/// <summary>
/// Arama sonucundaki bir sayfaya tıklanınca tam içeriği göstermek için eklendi -
/// arama sadece bir chunk (kısa parça) döndürüyor, tam sayfayı görmek ayrı bir
/// çağrı gerektiriyor. GetWikiPagesQuery'deki AYNI görünürlük kuralı burada da
/// uygulanıyor - sayfa Id'sini bilmek onu görebilmek anlamına gelmiyor, bir
/// kullanıcı başka bir departmanın DepartmentOnly sayfasının Id'sini tahmin
/// etse bile null (404) alır.
/// </summary>
public record GetWikiPageByIdQuery(Guid PageId) : IRequest<WikiPageDto?>;
