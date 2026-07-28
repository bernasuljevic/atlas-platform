using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Atlas.IntegrationTests;

/// <summary>
/// AI modülü (PostgreSQL) InMemory'ye çevrilmedi (bkz. AtlasApiFactory.cs'teki
/// not) - bu testler gerçek Docker'daki Postgres+pgvector'a yazıyor/okuyor.
/// Her testin sorgu/içerik metninde benzersiz bir GUID'li terim kullanılması
/// bilinçli: FakeEmbeddingService kelime-hash tabanlı olduğu için, veritabanında
/// önceki testlerden kalan onlarca alakasız chunk olsa bile, gerçekten eşleşen
/// (ortak kelimesi olan) tek sayfa her zaman en düşük mesafeyle (en yüksek
/// skorla) döner - bu yüzden TopN=5 gibi küçük bir sınırda bile güvenilir çıkar.
///
/// Her test kendi oluşturduğu sayfaların embedding'lerini try/finally ile
/// temizliyor (bkz. AiEmbeddingTestCleanup) - WikiDbContext InMemory olduğu
/// için sayfa test bitince kayboluyor ama AI'ın Postgres'i kalıcı, temizlik
/// olmasa her çalıştırma geride "yetim" embedding bırakırdı (canlı doğrulanmış
/// bir sorun - IAsyncLifetime.DisposeAsync güvenilir çalışmadığı için burada
/// bilerek try/finally kullanılıyor, daha öngörülebilir).
///
/// Outbox Pattern Gün 3'ten (2026-07-28) itibaren ingestion artık SENKRON DEĞİL -
/// OutboxProcessor 5 saniyede bir çalışan bir arka plan işleyici, sayfa
/// oluşturma isteği döner dönmez embedding hazır OLMAYABİLİR. Bu yüzden
/// SearchTitlesWithRetryAsync ile "eventual consistency" bekleniyor (poll
/// interval'dan biraz fazla bir timeout ile) - bu testleri öncekinden yavaş
/// yapıyor ama gerçek davranışı doğru yansıtıyor.
/// </summary>
public class AiSearchEndpointsTests : IClassFixture<AtlasApiFactory>
{
    private readonly AtlasApiFactory _factory;
    private readonly HttpClient _client;

    public AiSearchEndpointsTests(AtlasApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterAndLoginAsync(string department)
    {
        var email = $"ai-search-test-{Guid.NewGuid()}@atlas.local";

        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            fullName = "AI Arama Test Kullanıcısı",
            password = "TestSifre123!",
            department
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "TestSifre123!" });
        var body = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private async Task<Guid> CreateWikiPageAsync(
        string token, string title, string content, string departmentName, string visibility)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/wiki/pages")
        {
            Content = JsonContent.Create(new { title, content, departmentName, visibility })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        return created.GetProperty("id").GetGuid();
    }

    private async Task<List<string?>> SearchTitlesAsync(string token, string queryText)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get, $"/api/ai/search?q={Uri.EscapeDataString(queryText)}&topN=5");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<JsonElement>();
        return results.EnumerateArray().Select(r => r.GetProperty("title").GetString()).ToList();
    }

    // OutboxProcessor'ın PollInterval'ı (5sn) + biraz pay - embedding hazır olana
    // kadar 500ms aralıklarla tekrar arıyor. Süre dolduğunda son sonucu döndürüyor,
    // testin kendi Assert'i (Contains/DoesNotContain) anlamlı bir hata verecek.
    private async Task<List<string?>> SearchTitlesWithRetryAsync(string token, string queryText, string expectedTitle)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        List<string?> titles;
        do
        {
            titles = await SearchTitlesAsync(token, queryText);
            if (titles.Contains(expectedTitle))
                return titles;

            await Task.Delay(500);
        } while (DateTime.UtcNow < deadline);

        return titles;
    }

    [Fact]
    public async Task SayfaOlusturulunca_OtomatikEmbedEdilirVeAramadaCikar()
    {
        var token = await RegisterAndLoginAsync("IT");
        var uniqueTerm = $"kuantumfiltresi{Guid.NewGuid():N}";
        var title = $"Uçtan Uca Arama Testi {uniqueTerm}";
        Guid pageId = default;

        try
        {
            pageId = await CreateWikiPageAsync(
                token, title, $"Bu sayfa {uniqueTerm} konusunda detaylı bilgi içeriyor.", "IT", "Public");

            var titles = await SearchTitlesWithRetryAsync(token, uniqueTerm, title);

            Assert.Contains(title, titles);
        }
        finally
        {
            await AiEmbeddingTestCleanup.DeleteEmbeddingsForPagesAsync(_factory, [pageId]);
        }
    }

    [Fact]
    public async Task DepartmentOnlySayfa_AramaSonucunda_FarkliDepartmandakiKullanicidanGizlenir()
    {
        var ownerToken = await RegisterAndLoginAsync("IK");
        var uniqueTerm = $"gizlibordrupolitikasi{Guid.NewGuid():N}";
        var title = $"IK'ya Özel Test Sayfası {uniqueTerm}";
        Guid pageId = default;

        try
        {
            pageId = await CreateWikiPageAsync(
                ownerToken, title, $"{uniqueTerm} sadece IK departmanına özeldir.", "IK", "DepartmentOnly");

            // Önce indekslendiğini KANITLIYORUZ (sahibi görebilmeli) - aksi halde
            // aşağıdaki "başkası göremiyor" kontrolü YANLIŞ sebepten (henüz
            // işlenmediği için) geçmiş olabilirdi, gerçek görünürlük filtresini
            // değil.
            var ownerTitles = await SearchTitlesWithRetryAsync(ownerToken, uniqueTerm, title);
            Assert.Contains(title, ownerTitles);

            var otherDepartmentToken = await RegisterAndLoginAsync("IT");
            var otherTitles = await SearchTitlesAsync(otherDepartmentToken, uniqueTerm);
            Assert.DoesNotContain(title, otherTitles);
        }
        finally
        {
            await AiEmbeddingTestCleanup.DeleteEmbeddingsForPagesAsync(_factory, [pageId]);
        }
    }

    [Fact]
    public async Task BosSorguIle_400DonerVeAlanBazliHataVerir()
    {
        var token = await RegisterAndLoginAsync("IT");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/ai/search?q=");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TokenOlmadan_401Doner()
    {
        var response = await _client.GetAsync("/api/ai/search?q=herhangi");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
