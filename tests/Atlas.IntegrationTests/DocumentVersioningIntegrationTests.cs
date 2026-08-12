using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Atlas.Modules.Documents.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.IntegrationTests;

/// <summary>
/// P6 (belge versiyonlama + toplu yükleme) - "yeni versiyon yükle" akışını
/// (eski dosyanın DocumentVersion'a arşivlenmesi, indirilebilir kalması,
/// owner-or-Admin/"hâlâ işleniyor" guard'ları) ve ContentHash tabanlı
/// duplicate-detection'ı (görünürlük filtresiyle birlikte) uçtan uca
/// doğrulayan testler. DocumentsProcessingIntegrationTests'teki AYNI
/// desenler: sahip token'ı sınıf seviyesinde önbelleğe alınıyor (login rate
/// limit'i, bkz. o dosyadaki not), DocumentsDbContext InMemory.
/// </summary>
[Trait("Category", "Integration")]
public class DocumentVersioningIntegrationTests : IClassFixture<AtlasApiFactory>
{
    private static Task<string>? _ownerTokenTask;

    private readonly AtlasApiFactory _factory;
    private readonly HttpClient _client;

    public DocumentVersioningIntegrationTests(AtlasApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private Task<string> GetOwnerTokenAsync() =>
        _ownerTokenTask ??= AuthTestHelper.RegisterVerifyAndLoginAsync(
            _client, _factory, "Versiyonlama Test Kullanıcısı", "TestSifre123!", "IT");

    private async Task<JsonElement> UploadDocumentAsync(
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
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<HttpResponseMessage> UploadNewVersionAsync(string token, Guid documentId, string content, string fileName = "v2.txt")
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(fileContent, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/documents/{documentId}/versions") { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<JsonElement> GetDocumentAsync(Guid id)
    {
        var response = await _client.GetAsync($"/api/documents/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement> GetVersionsAsync(Guid id)
    {
        var response = await _client.GetAsync($"/api/documents/{id}/versions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task DeleteDocumentAsync(string token, Guid id)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/documents/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.SendAsync(request);
    }

    [Fact]
    public async Task YeniVersiyonYuklenince_EskiDosyaVersiyonGecmisineTasinirVeIndirilebilirKalir()
    {
        var token = await GetOwnerTokenAsync();
        Guid? documentId = null;

        try
        {
            var uploadResult = await UploadDocumentAsync(token, $"Versiyon Testi {Guid.NewGuid()}", "birinci içerik", "v1.txt");
            documentId = uploadResult.GetProperty("id").GetGuid();

            var versionResponse = await UploadNewVersionAsync(token, documentId.Value, "ikinci içerik", "v2.txt");
            Assert.Equal(HttpStatusCode.Accepted, versionResponse.StatusCode);

            // Document artık versiyon 2'yi işaret ediyor olmalı (bkz.
            // Document.ReplaceFile - CurrentVersionNumber += 1).
            var document = await GetDocumentAsync(documentId.Value);
            Assert.Equal(2, document.GetProperty("currentVersionNumber").GetInt32());

            // Versiyon geçmişi SADECE eski (1.) versiyonu içermeli - orijinal
            // dosya adıyla (bkz. GetDocumentVersionsQuery'deki "sadece eski
            // versiyonlar" notu).
            var versions = await GetVersionsAsync(documentId.Value);
            var version1 = Assert.Single(versions.EnumerateArray());
            Assert.Equal(1, version1.GetProperty("versionNumber").GetInt32());
            Assert.Equal("v1.txt", version1.GetProperty("originalFileName").GetString());

            // Eski versiyon HÂLÂ indirilebilir olmalı - dosyası diskte kalmaya
            // devam ediyor (bkz. UploadNewDocumentVersionCommandHandler'daki
            // "eski dosyayı SİLME" notu).
            var downloadRequest = new HttpRequestMessage(
                HttpMethod.Get, $"/api/documents/{documentId}/versions/1/download");
            downloadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var downloadResponse = await _client.SendAsync(downloadRequest);
            Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);

            var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
            Assert.Equal("birinci içerik", Encoding.UTF8.GetString(downloadedBytes));
        }
        finally
        {
            if (documentId is not null)
                await DeleteDocumentAsync(token, documentId.Value);
        }
    }

    [Fact]
    public async Task YeniVersiyonYukleme_SahibiOlmayanKullanici_403Aliyor()
    {
        var ownerToken = await GetOwnerTokenAsync();
        var otherToken = await AuthTestHelper.RegisterVerifyAndLoginAsync(
            _client, _factory, "Versiyon Yetki Testi Kullanıcısı", "TestSifre123!", "IK");
        Guid? documentId = null;

        try
        {
            var uploadResult = await UploadDocumentAsync(ownerToken, $"Yetki Testi {Guid.NewGuid()}", "içerik");
            documentId = uploadResult.GetProperty("id").GetGuid();

            var versionResponse = await UploadNewVersionAsync(otherToken, documentId.Value, "başkasının içeriği");
            Assert.Equal(HttpStatusCode.Forbidden, versionResponse.StatusCode);
        }
        finally
        {
            if (documentId is not null)
                await DeleteDocumentAsync(ownerToken, documentId.Value);
        }
    }

    [Fact]
    public async Task YeniVersiyonYukleme_HalaIslenmekteOlanBelgeye_400Aliyor()
    {
        var token = await GetOwnerTokenAsync();
        Guid? documentId = null;

        try
        {
            var uploadResult = await UploadDocumentAsync(token, $"Devam Eden İşlem Testi {Guid.NewGuid()}", "içerik");
            documentId = uploadResult.GetProperty("id").GetGuid();

            // ReprocessDocumentCommandHandler testindeki AYNI deterministik
            // yaklaşım - OutboxProcessor'ın zamanlamasına güvenmek yerine
            // Domain metodunu doğrudan çağırıp durumu sabitliyoruz.
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();
                var document = await db.Documents.FirstAsync(d => d.Id == documentId.Value);
                document.MarkExtracting();
                await db.SaveChangesAsync();
            }

            var versionResponse = await UploadNewVersionAsync(token, documentId.Value, "yeni içerik");
            Assert.Equal(HttpStatusCode.BadRequest, versionResponse.StatusCode);
        }
        finally
        {
            if (documentId is not null)
                await DeleteDocumentAsync(token, documentId.Value);
        }
    }

    [Fact]
    public async Task AyniIcerikliBelgeYuklenince_GorunurBirEslesmeVarsaUyariDoner()
    {
        var token = await GetOwnerTokenAsync();
        var sharedContent = $"tekrarlanan içerik {Guid.NewGuid()}";
        var firstTitle = $"İlk Belge {Guid.NewGuid()}";
        Guid? firstDocumentId = null;
        Guid? secondDocumentId = null;

        try
        {
            var first = await UploadDocumentAsync(token, firstTitle, sharedContent, "ilk.txt");
            firstDocumentId = first.GetProperty("id").GetGuid();

            var second = await UploadDocumentAsync(token, $"İkinci Belge {Guid.NewGuid()}", sharedContent, "ikinci.txt");
            secondDocumentId = second.GetProperty("id").GetGuid();

            // Yükleme YİNE DE başarılı (engellenmedi) - sadece uyarı bilgisi
            // taşıyor (bkz. UploadDocumentResult'taki not).
            Assert.Equal(firstDocumentId.Value, second.GetProperty("duplicateOfDocumentId").GetGuid());
            Assert.Equal(firstTitle, second.GetProperty("duplicateOfTitle").GetString());
        }
        finally
        {
            if (firstDocumentId is not null)
                await DeleteDocumentAsync(token, firstDocumentId.Value);
            if (secondDocumentId is not null)
                await DeleteDocumentAsync(token, secondDocumentId.Value);
        }
    }

    [Fact]
    public async Task AyniIcerikliBelge_FarkliDepartmandanGizliyse_UyariGelmez()
    {
        var ikToken = await AuthTestHelper.RegisterVerifyAndLoginAsync(
            _client, _factory, "Duplicate IK Testi Kullanıcısı", "TestSifre123!", "IK");
        var itToken = await AuthTestHelper.RegisterVerifyAndLoginAsync(
            _client, _factory, "Duplicate IT Testi Kullanıcısı", "TestSifre123!", "IT");
        var sharedContent = $"gizli tekrar {Guid.NewGuid()}";
        Guid? ikDocumentId = null;
        Guid? itDocumentId = null;

        try
        {
            // IK kullanıcısı DepartmentOnly bir belge yüklüyor - IT kullanıcısı
            // bunun VARLIĞINI BİLE bilmemeli (bkz. CLAUDE.md "Öğrenilen
            // dersler #10"daki aynı sınıf hata).
            var ikDoc = await UploadDocumentAsync(ikToken, $"IK Belgesi {Guid.NewGuid()}", sharedContent, "ik.txt", "DepartmentOnly");
            ikDocumentId = ikDoc.GetProperty("id").GetGuid();

            var itDoc = await UploadDocumentAsync(itToken, $"IT Belgesi {Guid.NewGuid()}", sharedContent, "it.txt", "Public");
            itDocumentId = itDoc.GetProperty("id").GetGuid();

            Assert.Equal(JsonValueKind.Null, itDoc.GetProperty("duplicateOfDocumentId").ValueKind);
        }
        finally
        {
            if (ikDocumentId is not null)
                await DeleteDocumentAsync(ikToken, ikDocumentId.Value);
            if (itDocumentId is not null)
                await DeleteDocumentAsync(itToken, itDocumentId.Value);
        }
    }
}
