using Atlas.Modules.AI.Application.Documents.Commands;
using Atlas.Modules.AI.Application.Tests.Fakes;

namespace Atlas.Modules.AI.Application.Tests;

public class DeleteDocumentEmbeddingsCommandHandlerTests
{
    [Fact]
    public async Task Handle_BelgeninTumEmbeddingleriniSilmesiIcinRepositoryyiCagirir()
    {
        var repository = new FakeDocumentEmbeddingRepository();
        var handler = new DeleteDocumentEmbeddingsCommandHandler(repository);
        var documentId = Guid.NewGuid();

        await handler.Handle(new DeleteDocumentEmbeddingsCommand(documentId), CancellationToken.None);

        Assert.Contains(documentId, repository.DeletedDocumentIds);
    }
}
