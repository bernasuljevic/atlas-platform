using System.Net;
using Atlas.Modules.AI.Domain.Entities;
using Atlas.Modules.AI.Infrastructure.Embeddings;
using Atlas.Modules.AI.Infrastructure.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Atlas.Modules.AI.Infrastructure.Tests;

// Bu testler gerçek bir Voyage AI API key'i GEREKTİRMİYOR - HttpClient,
// FakeHttpMessageHandler ile değiştirilerek gerçek ağa hiç çıkılmadan
// çalışıyor. Amaç: key gelip DI kaydı IEmbeddingService olarak
// VoyageEmbeddingService'e çevrildiği gün, bu sınıfın davranışının ZATEN
// doğrulanmış olması - o gün sadece bir "sağlayıcı bağlantısı" testi kalıyor,
// mantığın kendisi değil.
public class VoyageEmbeddingServiceTests
{
    private static (VoyageEmbeddingService Service, FakeHttpMessageHandler Handler) CreateService()
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.voyageai.com/v1/") };
        var options = Options.Create(new VoyageAiOptions { ApiKey = "test-key", Model = "voyage-3.5" });
        var service = new VoyageEmbeddingService(httpClient, options, NullLogger<VoyageEmbeddingService>.Instance);
        return (service, handler);
    }

    [Fact]
    public async Task EmbedAsync_BosListe_HicIstekAtmadanBosSonucDoner()
    {
        var (service, handler) = CreateService();

        var result = await service.EmbedAsync([]);

        Assert.Empty(result);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task EmbedAsync_BasariliIstek_VektorleriDogruSirayaKoyar()
    {
        var (service, handler) = CreateService();

        // Voyage'ın dönüş sırasının istek sırasıyla AYNI olacağına güvenmiyoruz -
        // bilerek TERS sırada ("index" alanına göre) bir cevap kuruyoruz, servisin
        // index'e göre doğru pozisyona yazdığını kanıtlamak için.
        handler.EnqueueJson(HttpStatusCode.OK, new
        {
            data = new object[]
            {
                new { embedding = Enumerable.Repeat(0.2f, EmbeddingDimensions.Standard).ToArray(), index = 1 },
                new { embedding = Enumerable.Repeat(0.1f, EmbeddingDimensions.Standard).ToArray(), index = 0 },
            },
        });

        var result = await service.EmbedAsync(["birinci metin", "ikinci metin"]);

        Assert.Equal(0.1f, result[0][0]);
        Assert.Equal(0.2f, result[1][0]);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task EmbedAsync_1000denFazlaMetin_BirdenFazlaIstegeBolunur()
    {
        var (service, handler) = CreateService();
        var texts = Enumerable.Range(0, 1001).Select(i => $"metin {i}").ToArray();

        // Voyage'ın tek istekte kabul ettiği üst sınır (1000) - bu yüzden 1001
        // metin İKİ isteğe bölünmeli: ilki 1000, ikincisi 1 metin taşımalı.
        handler.EnqueueJson(HttpStatusCode.OK, new
        {
            data = Enumerable.Range(0, 1000)
                .Select(i => new { embedding = Enumerable.Repeat(0.1f, EmbeddingDimensions.Standard).ToArray(), index = i })
                .ToArray(),
        });
        handler.EnqueueJson(HttpStatusCode.OK, new
        {
            data = new object[] { new { embedding = Enumerable.Repeat(0.2f, EmbeddingDimensions.Standard).ToArray(), index = 0 } },
        });

        var result = await service.EmbedAsync(texts);

        Assert.Equal(1001, result.Count);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task EmbedAsync_GeciciHata429_TekrarDenerVeBasariliOlur()
    {
        var (service, handler) = CreateService();
        handler.EnqueueError(HttpStatusCode.TooManyRequests);
        handler.EnqueueJson(HttpStatusCode.OK, new
        {
            data = new object[] { new { embedding = Enumerable.Repeat(0.3f, EmbeddingDimensions.Standard).ToArray(), index = 0 } },
        });

        var result = await service.EmbedAsync(["tek metin"]);

        Assert.Equal(0.3f, result[0][0]);
        // İlk deneme başarısız oldu (429), ikinci deneme başarılı - toplam 2 istek.
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task EmbedAsync_KaliciHata401_TekrarDenemedenHemenFirlatir()
    {
        var (service, handler) = CreateService();
        handler.EnqueueError(HttpStatusCode.Unauthorized, "geçersiz API key");

        await Assert.ThrowsAsync<VoyageEmbeddingException>(() => service.EmbedAsync(["tek metin"]));

        // 401 (kimlik doğrulama hatası) KALICI bir hata - retry mantığı bunu
        // "geçici" saymamalı, tek bir istekten sonra pes etmeli. Aksi halde
        // geçersiz bir key'le her çağrı 3 kat gereksiz istek atardı.
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task EmbedAsync_BeklenmeyenVektorBoyutu_HataFirlatir()
    {
        var (service, handler) = CreateService();
        handler.EnqueueJson(HttpStatusCode.OK, new
        {
            data = new object[] { new { embedding = new[] { 0.1f, 0.2f }, index = 0 } }, // 2 boyut, 1024 bekleniyor
        });

        // Ders #15'teki (sıfır-vektör -> NaN -> arama çöktü) sınıftan bir hatayı
        // pgvector'a ulaşmadan, burada erken yakalıyoruz - fail-fast.
        await Assert.ThrowsAsync<VoyageEmbeddingException>(() => service.EmbedAsync(["tek metin"]));
    }
}
