using Atlas.Modules.Wiki.Application.Tests.Fakes;
using Atlas.Shared.Testing;
using Atlas.Modules.Wiki.Application.WikiPages.Queries;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;

namespace Atlas.Modules.Wiki.Application.Tests;

public class GetWikiPageVersionByNumberQueryHandlerTests
{
    private static GetWikiPageVersionByNumberQueryHandler CreateHandler(
        FakeWikiPageRepository pageRepository,
        FakeWikiPageVersionRepository? versionRepository = null,
        string? viewerDepartment = "IT",
        bool viewerIsAdmin = false)
        => new(
            pageRepository,
            versionRepository ?? new FakeWikiPageVersionRepository(),
            new FakeCurrentUserAccessor(viewerDepartment, viewerIsAdmin));

    [Fact]
    public async Task VarOlanVersiyonun_TamIcerigini_Doner()
    {
        var pageRepository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Başlık", "İçerik", "IT", WikiVisibility.Public, Guid.NewGuid());
        pageRepository.AddedPages.Add(page);

        var versionRepository = new FakeWikiPageVersionRepository();
        versionRepository.Versions.Add(WikiPageVersion.CreateSnapshot(
            page.Id, 1, "Eski Başlık", "Eski içerik", "Public", "etiket1,etiket2", Guid.NewGuid(), "a@atlas.local"));

        var handler = CreateHandler(pageRepository, versionRepository);
        var result = await handler.Handle(new GetWikiPageVersionByNumberQuery(page.Id, 1), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Eski Başlık", result!.Title);
        Assert.Equal("Eski içerik", result.Content);
        Assert.Equal("etiket1,etiket2", result.Tags);
    }

    [Fact]
    public async Task OlmayanSayfaIcin_NullDoner()
    {
        var handler = CreateHandler(new FakeWikiPageRepository());
        var result = await handler.Handle(new GetWikiPageVersionByNumberQuery(Guid.NewGuid(), 1), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task VarOlanSayfadaOlmayanVersiyonNumarasi_NullDoner()
    {
        var pageRepository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Başlık", "İçerik", "IT", WikiVisibility.Public, Guid.NewGuid());
        pageRepository.AddedPages.Add(page);

        var handler = CreateHandler(pageRepository);
        var result = await handler.Handle(new GetWikiPageVersionByNumberQuery(page.Id, 99), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task BaskaDepartmaninDepartmentOnlySayfasi_VersiyonDetayindaDaGizlenir()
    {
        var pageRepository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Gizli Başlık", "Gizli içerik", "IK", WikiVisibility.DepartmentOnly, Guid.NewGuid());
        pageRepository.AddedPages.Add(page);

        var versionRepository = new FakeWikiPageVersionRepository();
        versionRepository.Versions.Add(WikiPageVersion.CreateSnapshot(
            page.Id, 1, "Gizli Başlık", "Gizli içerik", "DepartmentOnly", null, Guid.NewGuid(), "a@atlas.local"));

        var handler = CreateHandler(pageRepository, versionRepository, viewerDepartment: "IT");
        var result = await handler.Handle(new GetWikiPageVersionByNumberQuery(page.Id, 1), CancellationToken.None);

        Assert.Null(result);
    }
}
