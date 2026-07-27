using Atlas.Modules.AI.Domain.Entities;
using Atlas.Modules.AI.Infrastructure.Embeddings;
using Xunit;

namespace Atlas.Modules.AI.Infrastructure.Tests;

public class FakeEmbeddingServiceTests
{
    private readonly FakeEmbeddingService _sut = new();

    [Fact]
    public async Task HerVektor_BeklenenBoyuttaDoner()
    {
        var vectors = await _sut.EmbedAsync(new[] { "merhaba dünya" });

        Assert.Equal(WikiPageEmbedding.EmbeddingDimension, vectors[0].Length);
    }

    [Fact]
    public async Task AyniMetin_HerZamanAyniVektoruUretir()
    {
        // Bu, IEmbeddingService'in implicit "deterministik" beklentisini
        // doğruluyor - MD5 kullanmasaydık (string.GetHashCode() kullansaydık)
        // bu test AYNI process içinde geçebilirdi ama uygulama yeniden
        // başlatıldığında başarısız olurdu (biz burada süreç yeniden
        // başlatmayı simüle edemiyoruz, ama en azından aynı process içinde
        // iki farklı çağrının tutarlı olduğunu doğruluyoruz).
        var vector1 = (await _sut.EmbedAsync(new[] { "sunucu bakım prosedürü" }))[0];
        var vector2 = (await _sut.EmbedAsync(new[] { "sunucu bakım prosedürü" }))[0];

        Assert.Equal(vector1, vector2);
    }

    [Fact]
    public async Task FarkliMetinler_FarkliVektorUretir()
    {
        var vector1 = (await _sut.EmbedAsync(new[] { "sunucu bakım prosedürü" }))[0];
        var vector2 = (await _sut.EmbedAsync(new[] { "izin talebi formu" }))[0];

        Assert.NotEqual(vector1, vector2);
    }

    [Fact]
    public async Task BatchCagrisi_GirdiSirasiylaAyniSiradaVektorDoner()
    {
        var texts = new[] { "birinci metin", "ikinci metin", "üçüncü metin" };

        var batchVectors = await _sut.EmbedAsync(texts);
        var tekTekVectors = new List<float[]>();
        foreach (var text in texts)
            tekTekVectors.Add((await _sut.EmbedAsync(new[] { text }))[0]);

        for (var i = 0; i < texts.Length; i++)
            Assert.Equal(tekTekVectors[i], batchVectors[i]);
    }

    [Fact]
    public async Task VektorNormalizeEdilir_Buyuklugu1eYakinOlur()
    {
        var vector = (await _sut.EmbedAsync(new[] { "bu metin birkaç kelime içeriyor" }))[0];

        var magnitude = MathF.Sqrt(vector.Sum(v => v * v));

        Assert.InRange(magnitude, 0.999f, 1.001f);
    }

    [Fact]
    public async Task OrtakKelimesiCokOlanMetinler_BirbirineDahaYakinVektorUretir()
    {
        // Bu test, sahte servisin TAMAMEN rastgele değil, "feature hashing"
        // ile gerçekten anlamlı bir yakınlık ürettiğini kanıtlıyor - arama
        // özelliğini gerçek bir embedding modeli gelmeden de mantıklı şekilde
        // test edebilmemizin sebebi bu.
        var referans = "sunucu bakım prosedürü adımları";
        var benzer = "sunucu bakım prosedürü kontrol listesi";
        var alakasiz = "izin talebi formu doldurma rehberi";

        var vectors = await _sut.EmbedAsync(new[] { referans, benzer, alakasiz });

        var benzerlikBenzer = CosineSimilarity(vectors[0], vectors[1]);
        var benzerlikAlakasiz = CosineSimilarity(vectors[0], vectors[2]);

        Assert.True(benzerlikBenzer > benzerlikAlakasiz,
            $"Ortak kelimesi çok olan metinlerin benzerliği ({benzerlikBenzer}) " +
            $"alakasız metinden ({benzerlikAlakasiz}) yüksek olmalıydı.");
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        var dot = 0f;
        for (var i = 0; i < a.Length; i++)
            dot += a[i] * b[i];

        return dot;
    }
}
