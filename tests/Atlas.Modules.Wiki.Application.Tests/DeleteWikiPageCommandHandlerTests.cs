using Atlas.Modules.Wiki.Application.Tests.Fakes;
using Atlas.Modules.Wiki.Application.WikiPages.Commands;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;

namespace Atlas.Modules.Wiki.Application.Tests;

public class DeleteWikiPageCommandHandlerTests
{
    private static DeleteWikiPageCommandHandler CreateHandler(
        FakeWikiPageRepository repository, Guid? viewerUserId = null, bool viewerIsAdmin = false)
        => new(repository, new FakeCurrentUserAccessor("IT", viewerIsAdmin, viewerUserId), new FakePublisher());

    [Fact]
    public async Task SayfayiOlusturanKullanici_KendiSayfasiniSilebilir()
    {
        var ownerId = Guid.NewGuid();
        var repository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Başlık", "İçerik", "IT", WikiVisibility.Public, ownerId);
        repository.AddedPages.Add(page);

        var handler = CreateHandler(repository, viewerUserId: ownerId);
        await handler.Handle(new DeleteWikiPageCommand(page.Id), CancellationToken.None);

        Assert.Empty(repository.AddedPages);
    }

    [Fact]
    public async Task BaskasininSayfasiniSilmeyeCalisan_NormalKullanici_YetkisizHatasiAlir()
    {
        var repository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Başlık", "İçerik", "IT", WikiVisibility.Public, Guid.NewGuid());
        repository.AddedPages.Add(page);

        var handler = CreateHandler(repository, viewerUserId: Guid.NewGuid());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new DeleteWikiPageCommand(page.Id), CancellationToken.None));
        Assert.Single(repository.AddedPages);
    }

    [Fact]
    public async Task Admin_BaskasininSayfasiniDaSilebilir()
    {
        var repository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Başlık", "İçerik", "IT", WikiVisibility.Public, Guid.NewGuid());
        repository.AddedPages.Add(page);

        var handler = CreateHandler(repository, viewerUserId: Guid.NewGuid(), viewerIsAdmin: true);
        await handler.Handle(new DeleteWikiPageCommand(page.Id), CancellationToken.None);

        Assert.Empty(repository.AddedPages);
    }

    [Fact]
    public async Task OlmayanSayfayiSilmeyeCalisan_ArgumentExceptionAlir()
    {
        var repository = new FakeWikiPageRepository();
        var handler = CreateHandler(repository, viewerUserId: Guid.NewGuid());

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new DeleteWikiPageCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
