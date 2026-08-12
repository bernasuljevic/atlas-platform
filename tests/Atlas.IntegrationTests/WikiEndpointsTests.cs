using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Atlas.IntegrationTests;

// CI'da bu kategori atlanıyor (bkz. .github/workflows/ci.yml) - gerçek
// SQL Server/Postgres/Redis'e ihtiyaç duyduğu için, henüz servis
// container'larıyla CI'a taşınmadı (bkz. AtlasApiFactory'deki not).
[Trait("Category", "Integration")]
public class WikiEndpointsTests : IClassFixture<AtlasApiFactory>
{
    private readonly AtlasApiFactory _factory;
    private readonly HttpClient _client;

    public WikiEndpointsTests(AtlasApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // Departman ARTIK ZORUNLU - CreateWikiPageCommandHandler, normal bir
    // kullanıcının sayfasını her zaman kendi (gerçek, JWT'deki) departmanına
    // kaydediyor; departmansız bir kullanıcı sayfa oluşturamaz (bkz.
    // "Sayfa oluşturmak için bir departmana ait olmalısınız" hatası).
    private Task<string> RegisterAndLoginAsync() =>
        AuthTestHelper.RegisterVerifyAndLoginAsync(_client, _factory, "Wiki Test Kullanıcısı", "TestSifre123!", "IT");

    [Fact]
    public async Task SayfaOlustur_SonraListelendiginde_GorunurOluyor()
    {
        var accessToken = await RegisterAndLoginAsync();
        var title = $"Entegrasyon Test Sayfası {Guid.NewGuid()}";
        Guid? createdPageId = null;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/wiki/pages")
            {
                Content = JsonContent.Create(new
                {
                    title,
                    content = "WebApplicationFactory ile uçtan uca oluşturuldu",
                    departmentName = "IT",
                    visibility = "Public"
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var createResponse = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

            var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            createdPageId = created.GetProperty("id").GetGuid();

            var listResponse = await _client.GetAsync("/api/wiki/pages");
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

            // GET /api/wiki/pages artık sayfalanmış bir sonuç dönüyor: {items, pageNumber, pageSize, totalCount}.
            var result = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
            var found = result.GetProperty("items").EnumerateArray().Any(p => p.GetProperty("title").GetString() == title);

            Assert.True(found, $"'{title}' başlıklı sayfa listede bulunamadı.");
        }
        finally
        {
            // AI'ın Postgres'i test aralarında sıfırlanmıyor (bkz.
            // AiEmbeddingTestCleanup) - bu testin oluşturduğu sayfanın
            // embedding'ini burada temizliyoruz, aksi halde kalıcı bir
            // "yetim" olarak arama sonuçlarında hayalet gibi çıkabilirdi.
            if (createdPageId is not null)
                await AiEmbeddingTestCleanup.DeleteEmbeddingsForPagesAsync(_factory, [createdPageId.Value]);
        }
    }
}
