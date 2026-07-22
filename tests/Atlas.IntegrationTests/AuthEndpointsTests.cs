using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Atlas.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<AtlasApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(AtlasApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Sonra_Login_BasariylaTokenDoner()
    {
        var email = $"test-{Guid.NewGuid()}@atlas.local";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            fullName = "Entegrasyon Testi",
            password = "TestSifre123!"
        });

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "TestSifre123!"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var body = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("refreshToken").GetString()));
    }

    [Fact]
    public async Task YanlisSifreyle_Login_401Doner()
    {
        var email = $"test-{Guid.NewGuid()}@atlas.local";

        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            fullName = "Entegrasyon Testi",
            password = "DogruSifre123!"
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "YanlisSifre123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task TokensizIstek_KorumaliEndpointeErisemez()
    {
        // POST /api/wiki/pages .RequireAuthorization() ile korunuyor -
        // Authorization header'ı hiç göndermeden isteği atıyoruz.
        var response = await _client.PostAsJsonAsync("/api/wiki/pages", new
        {
            title = "Yetkisiz Deneme",
            content = "Bu asla kaydedilmemeli",
            departmentName = "IT",
            visibility = "Public"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
