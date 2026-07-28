using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Modules.AI.Application.Tests.Fakes;
using Atlas.Shared.Testing;
using Atlas.Modules.AI.Application.WikiPages.Queries;
using Atlas.Modules.AI.Domain.Entities;

namespace Atlas.Modules.AI.Application.Tests;

public class SearchWikiPagesByMeaningQueryHandlerTests
{
    private static float[] AnyEmbedding() => new float[WikiPageEmbedding.EmbeddingDimension];

    private static WikiPageEmbedding Chunk(
        Guid wikiPageId, string chunkText, string title, string departmentName, string visibility, int chunkIndex = 0)
        => WikiPageEmbedding.Create(wikiPageId, chunkIndex, chunkText, title, departmentName, visibility, AnyEmbedding());

    private static SearchWikiPagesByMeaningQueryHandler CreateHandler(
        IReadOnlyList<WikiPageEmbeddingSearchHit> hits, string? viewerDepartment, bool viewerIsAdmin = false)
        => new(
            new FakeEmbeddingServiceForTests(),
            new FakeWikiPageEmbeddingRepository(hits),
            new FakeWikiVisibilityChecker(),
            new FakeCurrentUserAccessor(viewerDepartment, viewerIsAdmin));

    [Fact]
    public async Task PublicSayfa_ViewerFarkliDepartmandaOlsaBile_SonuclardaCikar()
    {
        var pageId = Guid.NewGuid();
        var hits = new[]
        {
            new WikiPageEmbeddingSearchHit(
                Chunk(pageId, "sunucu bakım prosedürü", "Bakım Rehberi", "IK", "Public"), Distance: 0.1),
        };
        var handler = CreateHandler(hits, viewerDepartment: "IT");

        var result = await handler.Handle(new SearchWikiPagesByMeaningQuery("bakım"), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(pageId, result[0].WikiPageId);
    }

    [Fact]
    public async Task DepartmentOnlySayfa_ViewerFarkliDepartmandaysa_Filtrelenir()
    {
        // Bu test, Gün 4'ün ASIL sebebi olan güvenlik kuralını doğruluyor
        // (bkz. CLAUDE.md "Öğrenilen dersler #10") - departman görünürlük kuralına
        // uymayan bir chunk, aday havuzunda olsa bile sonuçta ASLA çıkmamalı.
        var hits = new[]
        {
            new WikiPageEmbeddingSearchHit(
                Chunk(Guid.NewGuid(), "IK'ya özel maaş politikası", "Maaş Politikası", "IK", "DepartmentOnly"), Distance: 0.05),
        };
        var handler = CreateHandler(hits, viewerDepartment: "IT");

        var result = await handler.Handle(new SearchWikiPagesByMeaningQuery("maaş"), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task DepartmentOnlySayfa_ViewerAyniDepartmandaysa_SonuclardaCikar()
    {
        var pageId = Guid.NewGuid();
        var hits = new[]
        {
            new WikiPageEmbeddingSearchHit(
                Chunk(pageId, "IK'ya özel maaş politikası", "Maaş Politikası", "IK", "DepartmentOnly"), Distance: 0.05),
        };
        var handler = CreateHandler(hits, viewerDepartment: "IK");

        var result = await handler.Handle(new SearchWikiPagesByMeaningQuery("maaş"), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(pageId, result[0].WikiPageId);
    }

    [Fact]
    public async Task DepartmentOnlySayfa_ViewerAdminIse_FarkliDepartmandaOlsaBileSonuclardaCikar()
    {
        // Admin, departman sınırını görmezden gelebiliyor (bilinçli tasarım kararı -
        // bkz. WikiVisibilityRules) - kurumsal bir sistemde IT desteği/üst yönetimin
        // tüm departmanların içeriğini görebilmesi makul bir beklenti.
        var pageId = Guid.NewGuid();
        var hits = new[]
        {
            new WikiPageEmbeddingSearchHit(
                Chunk(pageId, "IK'ya özel maaş politikası", "Maaş Politikası", "IK", "DepartmentOnly"), Distance: 0.05),
        };
        var handler = CreateHandler(hits, viewerDepartment: "IT", viewerIsAdmin: true);

        var result = await handler.Handle(new SearchWikiPagesByMeaningQuery("maaş"), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(pageId, result[0].WikiPageId);
    }

    [Fact]
    public async Task AyniSayfadanBirdenFazlaChunk_SonucSayfaBasinaBirKezCikarVeEnYakinChunkSecilir()
    {
        var pageId = Guid.NewGuid();
        var hits = new[]
        {
            new WikiPageEmbeddingSearchHit(
                Chunk(pageId, "uzak chunk", "Sayfa", "IT", "Public", chunkIndex: 0), Distance: 0.8),
            new WikiPageEmbeddingSearchHit(
                Chunk(pageId, "yakın chunk", "Sayfa", "IT", "Public", chunkIndex: 1), Distance: 0.1),
        };
        var handler = CreateHandler(hits, viewerDepartment: "IT");

        var result = await handler.Handle(new SearchWikiPagesByMeaningQuery("sorgu"), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("yakın chunk", result[0].ChunkText);
    }

    [Fact]
    public async Task Sonuclar_BenzerlikSirasinaGoreDiziliyor()
    {
        var uzakSayfa = Guid.NewGuid();
        var yakinSayfa = Guid.NewGuid();
        var hits = new[]
        {
            new WikiPageEmbeddingSearchHit(Chunk(uzakSayfa, "x", "Uzak Sayfa", "IT", "Public"), Distance: 0.9),
            new WikiPageEmbeddingSearchHit(Chunk(yakinSayfa, "y", "Yakın Sayfa", "IT", "Public"), Distance: 0.1),
        };
        var handler = CreateHandler(hits, viewerDepartment: "IT");

        var result = await handler.Handle(new SearchWikiPagesByMeaningQuery("sorgu"), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(yakinSayfa, result[0].WikiPageId);
        Assert.Equal(uzakSayfa, result[1].WikiPageId);
        Assert.True(result[0].Score > result[1].Score);
    }

    [Fact]
    public async Task TopN_SonucSayisiniSinirlar()
    {
        var hits = Enumerable.Range(0, 10)
            .Select(i => new WikiPageEmbeddingSearchHit(
                Chunk(Guid.NewGuid(), $"chunk {i}", $"Sayfa {i}", "IT", "Public"), Distance: i * 0.01))
            .ToArray();
        var handler = CreateHandler(hits, viewerDepartment: "IT");

        var result = await handler.Handle(new SearchWikiPagesByMeaningQuery("sorgu", TopN: 3), CancellationToken.None);

        Assert.Equal(3, result.Count);
    }
}
