using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Atlas.Modules.Documents.Infrastructure.Persistence;
using Atlas.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.IntegrationTests;

/// <summary>
/// P4 Gün 6 - belge işleme pipeline'ını (Upload -> Outbox -> DocumentUploadedEventHandler
/// -> Extracting/Ready/Failed) ve ReprocessDocumentCommand'ı (Gün 5) uçtan uca
/// doğrulayan testler. OutboxIntegrationTests/AiSearchEndpointsTests'teki AYNI
/// iki desen burada da geçerli:
///
/// 1. Atomiklik testi - HTTP yanıtı döner dönmez (OutboxProcessor'ın uyanmasını
///    BEKLEMEDEN) OutboxMessage satırının Document'in KENDİSİYLE aynı transaction'da
///    yazıldığını doğrudan DB'den kontrol ediyoruz.
/// 2. Eventual-consistency testi - gerçek extraction (PlainTextDocumentProcessor)
///    ve Outbox'ın 5sn'lik poll aralığını bekleyen bir retry helper.
///
/// DocumentsDbContext bu test host'unda InMemory (bkz. AtlasApiFactory) - ama
/// IFileStorageService (LocalDiskFileStorageService) BİLEREK gerçek diske
/// yazıyor, çünkü IDocumentProcessor'ların gerçek dosya içeriğini okuyabilmesi
/// gerekiyor. Testler oluşturdukları belgeleri silerek (DeleteDocumentCommandHandler)
/// diskteki dosyayı da temizliyor.
///
/// Sahip token'ı SINIF SEVİYESİNDE (static, tembel/lazy) ÖNBELLEKLENİYOR - "login"
/// rate limit politikası dakikada 5 istek (bkz. Program.cs), bu sınıftaki 7 testin
/// HER BİRİ kendi register+login'ini yapsaydı aynı 1 dakikalık pencerede kolayca
/// aşılırdı (AiSearchEndpointsTests'in 4 çağrıyla sınırda kalmasıyla AYNI kısıt).
/// Testlerin çoğu "kim yüklediği" ile ilgilenmediği için tek bir paylaşılan sahip
/// yeterli - SADECE yetki testi (owner-or-admin) gerçekten İKİNCİ, farklı bir
/// kullanıcı istiyor.
/// </summary>
[Trait("Category", "Integration")]
public class DocumentsProcessingIntegrationTests : IClassFixture<AtlasApiFactory>
{
    private static Task<string>? _ownerTokenTask;

    private readonly AtlasApiFactory _factory;
    private readonly HttpClient _client;

    public DocumentsProcessingIntegrationTests(AtlasApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private Task<string> GetOwnerTokenAsync() =>
        _ownerTokenTask ??= AuthTestHelper.RegisterVerifyAndLoginAsync(
            _client, _factory, "Documents Test Kullanıcısı", "TestSifre123!", "IT");

    private async Task<HttpResponseMessage> UploadDocumentAsync(
        string token, string title, string content, string fileName = "test.txt", string visibility = "Public")
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(title), "title");
        form.Add(new StringContent(visibility), "visibility");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/documents/upload") { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<JsonElement> GetDocumentAsync(Guid id)
    {
        var response = await _client.GetAsync($"/api/documents/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    // OutboxProcessor'ın PollInterval'ı (5sn) + biraz pay - AiSearchEndpointsTests'teki
    // SearchTitlesWithRetryAsync ile AYNI fikir, burada "Uploaded/Extracting"
    // dışında bir duruma (Ready ya da Failed) geçmesini bekliyoruz.
    private async Task<JsonElement> GetDocumentUntilProcessedAsync(Guid id)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        JsonElement document;
        do
        {
            document = await GetDocumentAsync(id);
            var status = document.GetProperty("status").GetString();
            if (status is "Ready" or "Failed")
                return document;

            await Task.Delay(500);
        } while (DateTime.UtcNow < deadline);

        return document;
    }

    private async Task DeleteDocumentAsync(string token, Guid id)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/documents/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.SendAsync(request);
    }

    [Fact]
    public async Task BelgeYuklenince_AyniAndaOutboxMesajiDaYaziliyor()
    {
        var token = await GetOwnerTokenAsync();
        Guid? documentId = null;

        try
        {
            var response = await UploadDocumentAsync(token, $"Outbox Atomiklik Testi {Guid.NewGuid()}", "İçerik önemli değil.");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<JsonElement>();
            documentId = created.GetProperty("id").GetGuid();

            // HTTP yanıtı döndüğü anda (arka plan işleyicinin uyanmasını
            // BEKLEMEDEN) OutboxMessage satırı zaten DB'de olmalı - UploadDocumentCommandHandler'ın
            // Document + OutboxMessage'ı TEK SaveChanges'te yazdığının doğrudan kanıtı.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();

            var documentIdText = documentId.Value.ToString();
            var outboxMessage = await db.OutboxMessages
                .Where(m => m.EventType.Contains(nameof(DocumentUploadedEvent)))
                .Where(m => m.Payload.Contains(documentIdText))
                .FirstOrDefaultAsync();

            Assert.NotNull(outboxMessage);
            Assert.Equal(0, outboxMessage!.Attempts);
        }
        finally
        {
            if (documentId is not null)
                await DeleteDocumentAsync(token, documentId.Value);
        }
    }

    [Fact]
    public async Task GecerliMetinBelgesi_IslenipReadyDurumunaGeciyorVeChunksReadyOlayiYaziliyor()
    {
        var token = await GetOwnerTokenAsync();
        var uniqueTerm = $"belgeislemetesti{Guid.NewGuid():N}";
        Guid? documentId = null;

        try
        {
            var response = await UploadDocumentAsync(
                token, $"Metin İşleme Testi {Guid.NewGuid()}",
                $"Bu belge {uniqueTerm} konusunu içeriyor ve düz metin olarak işlenebilir.");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<JsonElement>();
            documentId = created.GetProperty("id").GetGuid();

            var document = await GetDocumentUntilProcessedAsync(documentId.Value);
            Assert.Equal("Ready", document.GetProperty("status").GetString());

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();

            var documentIdText = documentId.Value.ToString();
            var chunksReadyMessage = await db.OutboxMessages
                .Where(m => m.EventType.Contains(nameof(DocumentChunksReadyEvent)))
                .Where(m => m.Payload.Contains(documentIdText))
                .Where(m => m.Payload.Contains(uniqueTerm))
                .FirstOrDefaultAsync();

            Assert.NotNull(chunksReadyMessage);
        }
        finally
        {
            if (documentId is not null)
                await DeleteDocumentAsync(token, documentId.Value);
        }
    }

    [Fact]
    public async Task IcerigiSadeceBosluklardanOlusanBelge_FailedDurumunaGeciyor()
    {
        var token = await GetOwnerTokenAsync();
        Guid? documentId = null;

        try
        {
            // PlainTextDocumentProcessor metni olduğu gibi okuyor - sadece
            // boşluk/satır sonu, DocumentUploadedEventHandler'daki
            // "IsNullOrWhiteSpace(extractedText)" kontrolüne takılıp Failed'a
            // düşmeli (bkz. Ders #15'teki AYNI sınıf hata - "anlamsız girdi"
            // durumu Documents pipeline'ında da düşünülmüş).
            var response = await UploadDocumentAsync(token, $"Boş İçerik Testi {Guid.NewGuid()}", "   \n   \n   ");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<JsonElement>();
            documentId = created.GetProperty("id").GetGuid();

            var document = await GetDocumentUntilProcessedAsync(documentId.Value);

            Assert.Equal("Failed", document.GetProperty("status").GetString());
            Assert.False(string.IsNullOrWhiteSpace(document.GetProperty("processingError").GetString()));
        }
        finally
        {
            if (documentId is not null)
                await DeleteDocumentAsync(token, documentId.Value);
        }
    }

    [Fact]
    public async Task Reprocess_SahibiOlmayanKullanici_403Aliyor()
    {
        var ownerToken = await GetOwnerTokenAsync();
        var otherToken = await AuthTestHelper.RegisterVerifyAndLoginAsync(
            _client, _factory, "Documents Yetki Testi Kullanıcısı", "TestSifre123!", "IK");
        Guid? documentId = null;

        try
        {
            var response = await UploadDocumentAsync(ownerToken, $"Yetki Testi {Guid.NewGuid()}", "İçerik.");
            var created = await response.Content.ReadFromJsonAsync<JsonElement>();
            documentId = created.GetProperty("id").GetGuid();

            var reprocessRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/documents/{documentId}/reprocess");
            reprocessRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
            var reprocessResponse = await _client.SendAsync(reprocessRequest);

            Assert.Equal(HttpStatusCode.Forbidden, reprocessResponse.StatusCode);
        }
        finally
        {
            if (documentId is not null)
                await DeleteDocumentAsync(ownerToken, documentId.Value);
        }
    }

    [Fact]
    public async Task Reprocess_OlmayanBelge_400Aliyor()
    {
        var token = await GetOwnerTokenAsync();

        var reprocessRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/documents/{Guid.NewGuid()}/reprocess");
        reprocessRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var reprocessResponse = await _client.SendAsync(reprocessRequest);

        Assert.Equal(HttpStatusCode.BadRequest, reprocessResponse.StatusCode);
    }

    [Fact]
    public async Task Reprocess_HalaIslenmekteOlanBelgeye_400Aliyor()
    {
        var token = await GetOwnerTokenAsync();
        Guid? documentId = null;

        try
        {
            var response = await UploadDocumentAsync(token, $"Devam Eden İşlem Testi {Guid.NewGuid()}", "İçerik.");
            var created = await response.Content.ReadFromJsonAsync<JsonElement>();
            documentId = created.GetProperty("id").GetGuid();

            // OutboxProcessor'ın 5sn'lik pencerede belgeyi gerçekten "Extracting"
            // durumunda yakalamasını beklemek flaky olurdu - bunun yerine Domain
            // metodunu (MarkExtracting) doğrudan çağırıp durumu deterministik
            // olarak sabitliyoruz, ReprocessDocumentCommandHandler'daki guard'ı
            // izole test ediyoruz.
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();
                var document = await db.Documents.FirstAsync(d => d.Id == documentId.Value);
                document.MarkExtracting();
                await db.SaveChangesAsync();
            }

            var reprocessRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/documents/{documentId}/reprocess");
            reprocessRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var reprocessResponse = await _client.SendAsync(reprocessRequest);

            Assert.Equal(HttpStatusCode.BadRequest, reprocessResponse.StatusCode);
        }
        finally
        {
            if (documentId is not null)
                await DeleteDocumentAsync(token, documentId.Value);
        }
    }

    [Fact]
    public async Task Reprocess_SahibiCagirinca_YeniBirOutboxMesajiKuyruklaniyor()
    {
        var token = await GetOwnerTokenAsync();
        Guid? documentId = null;

        try
        {
            var response = await UploadDocumentAsync(token, $"Reprocess Testi {Guid.NewGuid()}", "İçerik.");
            var created = await response.Content.ReadFromJsonAsync<JsonElement>();
            documentId = created.GetProperty("id").GetGuid();

            // İlk yükleme mesajının işlenmesini bekle (Ready/Failed fark etmez) -
            // asıl kontrol edilen şey reprocess'in KENDİ yeni mesajını yazması.
            await GetDocumentUntilProcessedAsync(documentId.Value);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();
            var documentIdText = documentId.Value.ToString();

            var messageCountBefore = await db.OutboxMessages
                .Where(m => m.EventType.Contains(nameof(DocumentUploadedEvent)))
                .Where(m => m.Payload.Contains(documentIdText))
                .CountAsync();

            var reprocessRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/documents/{documentId}/reprocess");
            reprocessRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var reprocessResponse = await _client.SendAsync(reprocessRequest);
            Assert.Equal(HttpStatusCode.Accepted, reprocessResponse.StatusCode);

            var messageCountAfter = await db.OutboxMessages
                .Where(m => m.EventType.Contains(nameof(DocumentUploadedEvent)))
                .Where(m => m.Payload.Contains(documentIdText))
                .CountAsync();

            Assert.Equal(messageCountBefore + 1, messageCountAfter);
        }
        finally
        {
            if (documentId is not null)
                await DeleteDocumentAsync(token, documentId.Value);
        }
    }
}
