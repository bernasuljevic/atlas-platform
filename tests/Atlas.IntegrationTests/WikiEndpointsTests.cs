using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Atlas.IntegrationTests;

public class WikiEndpointsTests : IClassFixture<AtlasApiFactory>
{
    private readonly HttpClient _client;

    public WikiEndpointsTests(AtlasApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var email = $"wiki-test-{Guid.NewGuid()}@atlas.local";

        // Departman ARTIK ZORUNLU - CreateWikiPageCommandHandler, normal bir
        // kullanıcının sayfasını her zaman kendi (gerçek, JWT'deki) departmanına
        // kaydediyor; departmansız bir kullanıcı sayfa oluşturamaz (bkz.
        // "Sayfa oluşturmak için bir departmana ait olmalısınız" hatası).
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            fullName = "Wiki Test Kullanıcısı",
            password = "TestSifre123!",
            department = "IT"
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "TestSifre123!"
        });

        var body = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    [Fact]
    public async Task SayfaOlustur_SonraListelendiginde_GorunurOluyor()
    {
        var accessToken = await RegisterAndLoginAsync();
        var title = $"Entegrasyon Test Sayfası {Guid.NewGuid()}";

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

        var listResponse = await _client.GetAsync("/api/wiki/pages");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        // GET /api/wiki/pages artık sayfalanmış bir sonuç dönüyor: {items, pageNumber, pageSize, totalCount}.
        var result = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var found = result.GetProperty("items").EnumerateArray().Any(p => p.GetProperty("title").GetString() == title);

        Assert.True(found, $"'{title}' başlıklı sayfa listede bulunamadı.");
    }
}
