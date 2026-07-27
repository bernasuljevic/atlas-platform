using Atlas.Modules.AI.Application.Tests.Fakes;
using Atlas.Modules.AI.Application.WikiPages.Commands;

namespace Atlas.Modules.AI.Application.Tests;

public class DeleteWikiPageEmbeddingsCommandHandlerTests
{
    [Fact]
    public async Task Handle_SayfaninTumEmbeddingleriniSilmesiIcinRepositoryyiCagirir()
    {
        var repository = new FakeWikiPageEmbeddingRepository();
        var handler = new DeleteWikiPageEmbeddingsCommandHandler(repository);
        var pageId = Guid.NewGuid();

        await handler.Handle(new DeleteWikiPageEmbeddingsCommand(pageId), CancellationToken.None);

        Assert.Contains(pageId, repository.DeletedWikiPageIds);
    }
}
