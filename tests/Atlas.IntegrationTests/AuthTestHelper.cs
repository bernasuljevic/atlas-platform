using System.Net.Http.Json;
using System.Text.Json;
using Atlas.Modules.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.IntegrationTests;

/// <summary>
/// Bu dosyanın var olma sebebi bir REGRESYON: e-posta doğrulama zorunluluğu
/// (bkz. LoginCommandHandler'daki `if (!user.EmailVerified)`) eklendiğinden
/// beri register+login yapan HER integration test dosyasının kendi
/// RegisterAndLoginAsync'i login'de 403 alıp kırılıyordu (bu proje P4'ün
/// bir parçası DEĞİL - Documents pipeline testleri yazılırken keşfedildi,
/// bkz. proje notu). Tek doğru yer burada, hepsi buna geçirildi.
///
/// Gerçek bir e-posta kutusu açmak yerine (AuthDbContext bu test host'unda
/// InMemory, bkz. AtlasApiFactory) doğrulama kodunu doğrudan DB'den okuyoruz -
/// RegisterUserCommandHandler'ın ürettiği KODUN KENDİSİ, ayrı bir "test modu"
/// kısayolu İCAT EDİLMEDİ.
/// </summary>
public static class AuthTestHelper
{
    public static async Task<string> RegisterVerifyAndLoginAsync(
        HttpClient client, AtlasApiFactory factory, string fullName, string password, string? department = null)
    {
        var email = $"{Guid.NewGuid()}@atlas.local";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new { email, fullName, password, department });
        registerResponse.EnsureSuccessStatusCode();
        var registered = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var userId = registered.GetProperty("id").GetGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var verificationCode = await db.EmailVerificationCodes
                .Where(c => c.UserId == userId && c.UsedAtUtc == null)
                .OrderByDescending(c => c.CreatedAtUtc)
                .FirstAsync();

            var verifyResponse = await client.PostAsJsonAsync(
                "/api/auth/verify-email", new { email, code = verificationCode.Code });
            verifyResponse.EnsureSuccessStatusCode();
        }

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();

        var body = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }
}
