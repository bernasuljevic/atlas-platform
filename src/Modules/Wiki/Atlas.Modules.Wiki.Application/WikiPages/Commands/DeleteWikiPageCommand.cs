using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

public record DeleteWikiPageCommand(Guid PageId) : IRequest, ICacheInvalidatingCommand
{
    public string CacheKeyToInvalidate => "wiki-pages:all";
}
