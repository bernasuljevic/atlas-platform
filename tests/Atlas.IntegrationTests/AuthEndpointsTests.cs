using System.Net;
using System.Net.Http.Json;

namespace Atlas.IntegrationTests;

[Trait("Category", "Integration")]
public class AuthEndpointsTests : IClassFixture<AtlasApiFactory>
{
    private readonly AtlasApiFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(AtlasApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_DogrulaSonraLogin_BasariylaTokenDoner()
    {
        // register/login/verify-email üçünü ayrı ayrı çağırmak yerine
        // AuthTestHelper kullanıyoruz - REGRESYON notu için bkz. AuthTestHelper.cs'deki
        // özet: e-posta doğrulama zorunluluğu eklendiğinde bu testin (ve register+login
        // yapan diğer tüm Integration testlerinin) eski hali 403 ile kırılıyordu.
        var accessToken = await AuthTestHelper.RegisterVerifyAndLoginAsync(
            _client, _factory, "Entegrasyon Testi", "TestSifre123!");

        Assert.False(string.IsNullOrWhiteSpace(accessToken));
    }

    [Fact]
    public async Task DogrulanmamisEposta_Login_403Doner()
    {
        // AuthTestHelper'ın atladığı (bilerek atladığı) senaryo - doğrulama
        // ADIMI YAPILMADAN login denenirse LoginCommandHandler'ın
        // EmailVerified kontrolüne takılıp 403 dönmesi gerekiyor (401 DEĞİL -
        // kimlik/şifre doğru, sadece işlem henüz yapılamıyor).
        var email = $"{Guid.NewGuid()}@atlas.local";

        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            fullName = "Entegrasyon Testi",
            password = "TestSifre123!"
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "TestSifre123!"
        });

        Assert.Equal(HttpStatusCode.Forbidden, loginResponse.StatusCode);
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

        // Şifre yanlışsa LoginCommandHandler doğrulama durumuna hiç bakmadan
        // null dönüyor (bkz. Handle metodundaki sıralama) - bu yüzden bu test
        // e-posta doğrulanmamış olsa bile hep 401 vermeye devam ediyor,
        // AuthTestHelper'a ihtiyacı yok.
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
