using Atlas.Modules.Wiki.Application.Tests.Fakes;
using Atlas.Shared.Testing;
using Atlas.Modules.Wiki.Application.WikiPages.Queries;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;

namespace Atlas.Modules.Wiki.Application.Tests;

public class GetWikiPageVersionsQueryHandlerTests
{
    private static GetWikiPageVersionsQueryHandler CreateHandler(
        FakeWikiPageRepository pageRepository,
        FakeWikiPageVersionRepository? versionRepository = null,
        string? viewerDepartment = "IT",
        bool viewerIsAdmin = false)
        => new(
            pageRepository,
            versionRepository ?? new FakeWikiPageVersionRepository(),
            new FakeCurrentUserAccessor(viewerDepartment, viewerIsAdmin));

    [Fact]
    public async Task GecmisVersiyonlariEnYeniden_EskiyeSirali_Doner()
    {
        var pageRepository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Başlık", "İçerik", "IT", WikiVisibility.Public, Guid.NewGuid());
        pageRepository.AddedPages.Add(page);

        var versionRepository = new FakeWikiPageVersionRepository();
        versionRepository.Versions.Add(WikiPageVersion.CreateSnapshot(
            page.Id, 1, "v1", "içerik", "Public", null, Guid.NewGuid(), "a@atlas.local"));
        versionRepository.Versions.Add(WikiPageVersion.CreateSnapshot(
            page.Id, 2, "v2", "içerik", "Public", null, Guid.NewGuid(), "a@atlas.local"));

        var handler = CreateHandler(pageRepository, versionRepository);
        var result = await handler.Handle(new GetWikiPageVersionsQuery(page.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(new[] { 2, 1 }, result!.Select(v => v.VersionNumber));
    }

    [Fact]
    public async Task OlmayanSayfaIcin_NullDoner()
    {
        var handler = CreateHandler(new FakeWikiPageRepository());
        var result = await handler.Handle(new GetWikiPageVersionsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task BaskaDepartmaninDepartmentOnlySayfasi_GorunmezVeyaVarligiGizlenir()
    {
        var pageRepository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Gizli Başlık", "Gizli içerik", "IK", WikiVisibility.DepartmentOnly, Guid.NewGuid());
        pageRepository.AddedPages.Add(page);

        // Görüntüleyen "IT" departmanında, sayfa "IK"ya DepartmentOnly - Id'yi
        // bilmek görebilmek anlamına gelmiyor (GetWikiPageByIdQueryHandler'daki
        // AYNI kural).
        var handler = CreateHandler(pageRepository, viewerDepartment: "IT");
        var result = await handler.Handle(new GetWikiPageVersionsQuery(page.Id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Admin_BaskaDepartmaninDepartmentOnlySayfasininVersiyonlarini_DaGorebilir()
    {
        var pageRepository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Gizli Başlık", "Gizli içerik", "IK", WikiVisibility.DepartmentOnly, Guid.NewGuid());
        pageRepository.AddedPages.Add(page);

        var handler = CreateHandler(pageRepository, viewerDepartment: "IT", viewerIsAdmin: true);
        var result = await handler.Handle(new GetWikiPageVersionsQuery(page.Id), CancellationToken.None);

        Assert.NotNull(result);
    }
}
