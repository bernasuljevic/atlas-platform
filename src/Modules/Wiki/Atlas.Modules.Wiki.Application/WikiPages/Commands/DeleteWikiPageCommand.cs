using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

public record DeleteWikiPageCommand(Guid PageId) : IRequest, ICacheInvalidatingCommand, IAuditableCommand
{
    public string CacheKeyToInvalidate => "wiki-pages:all";

    // Delete'te ID zaten Command'ın kendisinde biliniyor (Create'in aksine) -
    // AuditBehavior'ın TResponse'tan türetmesine gerek yok.
    public string AuditAction => "WikiPage.Deleted";
    public string? AuditResourceId => PageId.ToString();

    // Title'ı Command bilmiyor (sadece PageId var) - Handler, sayfayı silmeden
    // ÖNCE repository'den çektiğinde burayı dolduruyor (bkz.
    // DeleteWikiPageCommandHandler). Bunsuz, silinen bir sayfanın audit
    // kaydında geriye sadece anlamsız bir GUID kalırdı - sayfa artık Wiki'nin
    // veritabanında yok, başka hiçbir yerden title'a erişilemez.
    public string? AuditDetails { get; set; }
}
