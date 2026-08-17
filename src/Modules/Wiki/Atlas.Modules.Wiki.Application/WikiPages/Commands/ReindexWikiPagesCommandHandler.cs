using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

public class ReindexWikiPagesCommandHandler : IRequestHandler<ReindexWikiPagesCommand, int>
{
    private readonly IWikiPageRepository _wikiPageRepository;
    private readonly IPublisher _publisher;

    public ReindexWikiPagesCommandHandler(IWikiPageRepository wikiPageRepository, IPublisher publisher)
    {
        _wikiPageRepository = wikiPageRepository;
        _publisher = publisher;
    }

    public async Task<int> Handle(ReindexWikiPagesCommand request, CancellationToken cancellationToken)
    {
        var pages = await _wikiPageRepository.GetAllAsync(cancellationToken);

        foreach (var page in pages)
        {
            await _publisher.Publish(
                new WikiPageCreatedEvent(
                    page.Id, page.Title, page.DepartmentName, page.Content, page.Visibility.ToString(),
                    page.CreatedByEmail, IsReindexReplay: true),
                cancellationToken);
        }

        return pages.Count;
    }
}
