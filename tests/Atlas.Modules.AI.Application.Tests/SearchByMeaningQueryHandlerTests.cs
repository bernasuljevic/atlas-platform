using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Modules.AI.Application.Search.Queries;
using Atlas.Modules.AI.Application.Tests.Fakes;
using Atlas.Modules.AI.Domain.Entities;
using Atlas.Shared.Testing;

namespace Atlas.Modules.AI.Application.Tests;

// SearchWikiPagesByMeaningQueryHandlerTests'in (P5 öncesi) devamı - AYNI
// senaryolar korunuyor (görünürlük filtresi, kaynak başına tek chunk, sıralama,
// TopN), AMA artık İKİ kaynak da (Wiki + Documents) test ediliyor - Documents'a
// özel senaryolar bilerek Wiki'yle BİREBİR ayna (aynı görünürlük kuralı AYNI
// IWikiVisibilityChecker'dan geçiyor). Yeni birleşik-sonuç testleri EN SONDA.
public class SearchByMeaningQueryHandlerTests
{
    private static float[] AnyEmbedding() => new float[EmbeddingDimensions.Standard];

    private static WikiPageEmbedding WikiChunk(
        Guid wikiPageId, string chunkText, string title, string departmentName, string visibility, int chunkIndex = 0)
        => WikiPageEmbedding.Create(wikiPageId, chunkIndex, chunkText, title, departmentName, visibility, AnyEmbedding());

    private static DocumentEmbedding DocumentChunk(
        Guid documentId, string chunkText, string title, string departmentName, string visibility, int chunkIndex = 0)
        => DocumentEmbedding.Create(documentId, chunkIndex, chunkText, title, departmentName, visibility, AnyEmbedding());

    private static SearchByMeaningQueryHandler CreateHandler(
        string? viewerDepartment,
        bool viewerIsAdmin = false,
        IReadOnlyList<WikiPageEmbeddingSearchHit>? wikiHits = null,
        IReadOnlyList<DocumentEmbeddingSearchHit>? documentHits = null)
        => new(
            new FakeEmbeddingServiceForTests(),
            new FakeWikiPageEmbeddingRepository(wikiHits),
            new FakeDocumentEmbeddingRepository(documentHits),
            new FakeWikiVisibilityChecker(),
            new FakeCurrentUserAccessor(viewerDepartment, viewerIsAdmin));

    [Fact]
    public async Task PublicWikiSayfasi_ViewerFarkliDepartmandaOlsaBile_SonuclardaCikar()
    {
        var pageId = Guid.NewGuid();
        var hits = new[]
        {
            new WikiPageEmbeddingSearchHit(
                WikiChunk(pageId, "sunucu bakım prosedürü", "Bakım Rehberi", "IK", "Public"), Distance: 0.1),
        };
        var handler = CreateHandler(viewerDepartment: "IT", wikiHits: hits);

        var result = await handler.Handle(new SearchByMeaningQuery("bakım"), CancellationToken.None);

        var hit = Assert.Single(result);
        Assert.Equal(SearchResultSourceTypes.WikiPage, hit.SourceType);
        Assert.Equal(pageId, hit.ResourceId);
    }

    [Fact]
    public async Task DepartmentOnlyWikiSayfasi_ViewerFarkliDepartmandaysa_Filtrelenir()
    {
        var hits = new[]
        {
            new WikiPageEmbeddingSearchHit(
                WikiChunk(Guid.NewGuid(), "IK'ya özel maaş politikası", "Maaş Politikası", "IK", "DepartmentOnly"), Distance: 0.05),
        };
        var handler = CreateHandler(viewerDepartment: "IT", wikiHits: hits);

        var result = await handler.Handle(new SearchByMeaningQuery("maaş"), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task DepartmentOnlyBelge_ViewerFarkliDepartmandaysa_Filtrelenir()
    {
        // Bu test P5'in ASIL sebebi olan güvenlik kuralını doğruluyor (bkz.
        // CLAUDE.md "Öğrenilen dersler #10") - Documents'ın chunk'ları da AYNI
        // departman görünürlük kuralına tabi, Wiki'den farklı davranmıyor.
        var hits = new[]
        {
            new DocumentEmbeddingSearchHit(
                DocumentChunk(Guid.NewGuid(), "IK'ya özel bordro şablonu", "Bordro Şablonu", "IK", "DepartmentOnly"), Distance: 0.05),
        };
        var handler = CreateHandler(viewerDepartment: "IT", documentHits: hits);

        var result = await handler.Handle(new SearchByMeaningQuery("bordro"), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task DepartmentOnlyBelge_ViewerAyniDepartmandaysa_SonuclardaCikar()
    {
        var documentId = Guid.NewGuid();
        var hits = new[]
        {
            new DocumentEmbeddingSearchHit(
                DocumentChunk(documentId, "IK'ya özel bordro şablonu", "Bordro Şablonu", "IK", "DepartmentOnly"), Distance: 0.05),
        };
        var handler = CreateHandler(viewerDepartment: "IK", documentHits: hits);

        var result = await handler.Handle(new SearchByMeaningQuery("bordro"), CancellationToken.None);

        var hit = Assert.Single(result);
        Assert.Equal(SearchResultSourceTypes.Document, hit.SourceType);
        Assert.Equal(documentId, hit.ResourceId);
    }

    [Fact]
    public async Task DepartmentOnlyBelge_ViewerAdminIse_FarkliDepartmandaOlsaBileSonuclardaCikar()
    {
        var documentId = Guid.NewGuid();
        var hits = new[]
        {
            new DocumentEmbeddingSearchHit(
                DocumentChunk(documentId, "IK'ya özel bordro şablonu", "Bordro Şablonu", "IK", "DepartmentOnly"), Distance: 0.05),
        };
        var handler = CreateHandler(viewerDepartment: "IT", viewerIsAdmin: true, documentHits: hits);

        var result = await handler.Handle(new SearchByMeaningQuery("bordro"), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task AyniBelgedenBirdenFazlaChunk_SonucBelgeBasinaBirKezCikarVeEnYakinChunkSecilir()
    {
        var documentId = Guid.NewGuid();
        var hits = new[]
        {
            new DocumentEmbeddingSearchHit(
                DocumentChunk(documentId, "uzak chunk", "Belge", "IT", "Public", chunkIndex: 0), Distance: 0.8),
            new DocumentEmbeddingSearchHit(
                DocumentChunk(documentId, "yakın chunk", "Belge", "IT", "Public", chunkIndex: 1), Distance: 0.1),
        };
        var handler = CreateHandler(viewerDepartment: "IT", documentHits: hits);

        var result = await handler.Handle(new SearchByMeaningQuery("sorgu"), CancellationToken.None);

        var hit = Assert.Single(result);
        Assert.Equal("yakın chunk", hit.ChunkText);
    }

    [Fact]
    public async Task WikiVeDocumentSonuclari_TekListedeSkoraGoreBirlesikSiraliDoner()
    {
        // Asıl "birleşik arama" iddiasını doğrulayan test - iki AYRI kaynaktan
        // (Wiki + Documents) gelen sonuçlar TEK bir listede, skora göre doğru
        // sırada dönmeli, kaynak türüne göre AYRI gruplanmamalı.
        var yakinWikiSayfa = Guid.NewGuid();
        var uzakBelge = Guid.NewGuid();
        var wikiHits = new[]
        {
            new WikiPageEmbeddingSearchHit(
                WikiChunk(yakinWikiSayfa, "x", "Yakın Wiki Sayfası", "IT", "Public"), Distance: 0.1),
        };
        var documentHits = new[]
        {
            new DocumentEmbeddingSearchHit(
                DocumentChunk(uzakBelge, "y", "Uzak Belge", "IT", "Public"), Distance: 0.9),
        };
        var handler = CreateHandler(viewerDepartment: "IT", wikiHits: wikiHits, documentHits: documentHits);

        var result = await handler.Handle(new SearchByMeaningQuery("sorgu"), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(SearchResultSourceTypes.WikiPage, result[0].SourceType);
        Assert.Equal(yakinWikiSayfa, result[0].ResourceId);
        Assert.Equal(SearchResultSourceTypes.Document, result[1].SourceType);
        Assert.Equal(uzakBelge, result[1].ResourceId);
        Assert.True(result[0].Score > result[1].Score);
    }

    [Fact]
    public async Task TopN_IkiKaynaktanGelenToplamSonucSayisiniSinirlar()
    {
        var wikiHits = Enumerable.Range(0, 5)
            .Select(i => new WikiPageEmbeddingSearchHit(
                WikiChunk(Guid.NewGuid(), $"wiki {i}", $"Wiki Sayfa {i}", "IT", "Public"), Distance: i * 0.01))
            .ToArray();
        var documentHits = Enumerable.Range(0, 5)
            .Select(i => new DocumentEmbeddingSearchHit(
                DocumentChunk(Guid.NewGuid(), $"belge {i}", $"Belge {i}", "IT", "Public"), Distance: i * 0.01 + 0.5))
            .ToArray();
        var handler = CreateHandler(viewerDepartment: "IT", wikiHits: wikiHits, documentHits: documentHits);

        var result = await handler.Handle(new SearchByMeaningQuery("sorgu", TopN: 3), CancellationToken.None);

        Assert.Equal(3, result.Count);
        // En düşük mesafeli (en yüksek skorlu) 3 sonuç hep Wiki tarafından
        // geliyor olmalı (0.00-0.02), Documents'ın en yakın sonucu bile
        // (0.5) çok daha uzak.
        Assert.All(result, r => Assert.Equal(SearchResultSourceTypes.WikiPage, r.SourceType));
    }
}
