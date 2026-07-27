using Atlas.Modules.AI.Application.Abstractions;
using MediatR;

namespace Atlas.Modules.AI.Application.WikiPages.Commands;

public class DeleteWikiPageEmbeddingsCommandHandler : IRequestHandler<DeleteWikiPageEmbeddingsCommand>
{
    private readonly IWikiPageEmbeddingRepository _repository;

    public DeleteWikiPageEmbeddingsCommandHandler(IWikiPageEmbeddingRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteWikiPageEmbeddingsCommand request, CancellationToken cancellationToken)
    {
        await _repository.DeleteByWikiPageIdAsync(request.WikiPageId, cancellationToken);
    }
}
