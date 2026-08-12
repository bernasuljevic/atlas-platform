using Atlas.Modules.AI.Application.Abstractions;
using MediatR;

namespace Atlas.Modules.AI.Application.Documents.Commands;

public class DeleteDocumentEmbeddingsCommandHandler : IRequestHandler<DeleteDocumentEmbeddingsCommand>
{
    private readonly IDocumentEmbeddingRepository _repository;

    public DeleteDocumentEmbeddingsCommandHandler(IDocumentEmbeddingRepository repository)
    {
        _repository = repository;
    }

    public Task Handle(DeleteDocumentEmbeddingsCommand request, CancellationToken cancellationToken) =>
        _repository.DeleteByDocumentIdAsync(request.DocumentId, cancellationToken);
}
