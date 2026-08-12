using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Atlas.Modules.Wiki.Infrastructure.Persistence;
using Atlas.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.IntegrationTests;

/// <summary>
/// Transactional Outbox Pattern'in asıl iddiasını (WikiPage + OutboxMessage
/// AYNI SaveChanges'te, atomik yazılıyor - bkz. CreateWikiPageCommandHandler,
/// IUnitOfWork) doğrudan doğrulayan testler. Gerçek bir "process SaveChanges
/// ortasında çöktü" senaryosu integration testte simüle edilemez - ama HTTP
/// isteği 200 döndüğü anda hem WikiPage hem OutboxMessage'ın DB'de var olduğunu
/// kanıtlamak, atomikliğin test edilebilir kısmı.
///
/// ÖNEMLİ: OutboxProcessor bu test host'unda da gerçek bir BackgroundService
/// olarak çalışıyor - mesaj birkaç saniye içinde işlenip ProcessedAtUtc
/// dolabilir. Bu yüzden testler ProcessedAtUtc'nin null OLMASINI beklemiyor
/// (bu, işleyicinin ne zaman uyandığına bağlı, flaky bir varsayım olurdu) -
/// sadece satırın DOĞRU EventType/Payload ile VAR OLDUĞUNU doğruluyor, HTTP
/// yanıtı döner dönmez.
/// </summary>
[Trait("Category", "Integration")]
public class OutboxIntegrationTests : IClassFixture<AtlasApiFactory>
{
    private readonly AtlasApiFactory _factory;
    private readonly HttpClient _client;

    public OutboxIntegrationTests(AtlasApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private Task<string> RegisterAndLoginAsync() =>
        AuthTestHelper.RegisterVerifyAndLoginAsync(_client, _factory, "Outbox Test Kullanıcısı", "TestSifre123!", "IT");

    [Fact]
    public async Task SayfaOlusturulunca_AyniAndaOutboxMesajiDaYaziliyor()
    {
        var token = await RegisterAndLoginAsync();
        var title = $"Outbox Atomiklik Testi {Guid.NewGuid()}";
        Guid? createdPageId = null;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/wiki/pages")
            {
                Content = JsonContent.Create(new
                {
                    title,
                    content = "Outbox atomikliğini doğrulayan test sayfası.",
                    departmentName = "IT",
                    visibility = "Public"
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<JsonElement>();
            createdPageId = created.GetProperty("id").GetGuid();

            // HTTP yanıtı döndüğü anda (arka plan işleyicinin uyanmasını
            // BEKLEMEDEN) OutboxMessage satırı zaten DB'de olmalı - bu,
            // CreateWikiPageCommandHandler'ın WikiPage + OutboxMessage'ı
            // TEK SaveChanges'te yazdığının doğrudan kanıtı.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WikiDbContext>();

            var pageIdText = createdPageId.Value.ToString();
            var outboxMessage = await db.OutboxMessages
                .Where(m => m.EventType.Contains(nameof(WikiPageCreatedEvent)))
                .Where(m => m.Payload.Contains(pageIdText))
                .FirstOrDefaultAsync();

            Assert.NotNull(outboxMessage);
            Assert.Equal(0, outboxMessage!.Attempts);
        }
        finally
        {
            if (createdPageId is not null)
                await AiEmbeddingTestCleanup.DeleteEmbeddingsForPagesAsync(_factory, [createdPageId.Value]);
        }
    }

    [Fact]
    public async Task SayfaSilininceOlusanOutboxMesaji_DogruEventTipindeYaziliyor()
    {
        var token = await RegisterAndLoginAsync();
        var title = $"Outbox Silme Testi {Guid.NewGuid()}";

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/wiki/pages")
        {
            Content = JsonContent.Create(new
            {
                title,
                content = "Silinecek test sayfası.",
                departmentName = "IT",
                visibility = "Public"
            })
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createResponse = await _client.SendAsync(createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var pageId = created.GetProperty("id").GetGuid();

        try
        {
            var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/wiki/pages/{pageId}");
            deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var deleteResponse = await _client.SendAsync(deleteRequest);
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WikiDbContext>();

            var pageIdText = pageId.ToString();
            var outboxMessage = await db.OutboxMessages
                .Where(m => m.EventType.Contains(nameof(WikiPageDeletedEvent)))
                .Where(m => m.Payload.Contains(pageIdText))
                .FirstOrDefaultAsync();

            Assert.NotNull(outboxMessage);
        }
        finally
        {
            await AiEmbeddingTestCleanup.DeleteEmbeddingsForPagesAsync(_factory, [pageId]);
        }
    }
}
