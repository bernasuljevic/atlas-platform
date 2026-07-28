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
}
