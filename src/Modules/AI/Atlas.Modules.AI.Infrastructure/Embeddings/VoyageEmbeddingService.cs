using System.Net;
using System.Net.Http.Json;
using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Modules.AI.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Atlas.Modules.AI.Infrastructure.Embeddings;

/// <summary>
/// FakeEmbeddingService'in gerçek karşılığı - Voyage AI'ın "POST /v1/embeddings"
/// API'sine karşı çalışır. HENÜZ AIModule.cs'te IEmbeddingService olarak DI'a
/// BAĞLANMADI (Fake hâlâ aktif) - API key gelince tek satır değişecek:
/// `AddSingleton&lt;IEmbeddingService, FakeEmbeddingService&gt;()` ->
/// `AddSingleton&lt;IEmbeddingService, VoyageEmbeddingService&gt;()`, tasarımın
/// vaat ettiği gibi. Bu sınıfın kendisi API key'e ihtiyaç DUYMADAN yazılıp
/// test edilebiliyor - HttpClient sahte bir HttpMessageHandler'la
/// değiştirilerek (bkz. VoyageEmbeddingServiceTests) gerçek ağ çağrısı hiç
/// yapılmadan doğrulanıyor.
///
/// HttpClient, AIModule.cs'te bir "typed client" olarak (AddHttpClient&lt;
/// VoyageEmbeddingService&gt;) enjekte ediliyor - BaseAddress/Authorization
/// header'ı orada, Options'tan okunarak bir kere kuruluyor.
/// </summary>
public class VoyageEmbeddingService : IEmbeddingService
{
    // Voyage AI'ın tek istekte kabul ettiği maksimum metin sayısı (dokümantasyon,
    // 2026-08). Modele göre değişen TOPLAM token limitini (120K-1M) BİLEREK
    // ayrıca hesaplamıyoruz - "truncation: true" Voyage'ın aşırı uzun TEK bir
    // metni kendisinin kesmesini sağlıyor; toplam batch token bütçesini önceden
    // kestirmek bir tokenizer kütüphanesi eklemeyi gerektirirdi, şimdilik YAGNI -
    // gerçek kullanımda "too many tokens" hatası görülürse bu sabit küçültülür.
    private const int MaxTextsPerRequest = 1000;

    private const int MaxAttempts = 3;
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)];

    private readonly HttpClient _httpClient;
    private readonly VoyageAiOptions _options;
    private readonly ILogger<VoyageEmbeddingService> _logger;

    public VoyageEmbeddingService(HttpClient httpClient, IOptions<VoyageAiOptions> options, ILogger<VoyageEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
            return [];

        // Sıra garantisi (IEmbeddingService'in sözleşmesi: çıktı[i] = girdi[i])
        // Voyage'ın kendi döndürdüğü "index" alanına göre sağlanıyor - aynı
        // isteğin dönüş sırasının istek sırasıyla birebir aynı olacağına
        // güvenmek yerine (dokümante edilmiş bir garanti değil), her elemanı
        // kendi index'ine göre doğru pozisyona yazıyoruz.
        var results = new float[texts.Count][];
        var offset = 0;

        foreach (var batch in texts.Chunk(MaxTextsPerRequest))
        {
            var batchResults = await EmbedBatchWithRetryAsync(batch, cancellationToken);
            foreach (var (embedding, index) in batchResults)
                results[offset + index] = embedding;

            offset += batch.Length;
        }

        return results;
    }

    private async Task<IReadOnlyList<(float[] Embedding, int Index)>> EmbedBatchWithRetryAsync(
        string[] batch, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await EmbedBatchAsync(batch, cancellationToken);
            }
            catch (Exception ex) when (attempt < MaxAttempts && IsTransient(ex, cancellationToken))
            {
                var delay = RetryDelays[Math.Min(attempt - 1, RetryDelays.Length - 1)];
                _logger.LogWarning(ex,
                    "Voyage AI embedding isteği {Attempt}. denemede başarısız oldu, {DelaySeconds}sn sonra tekrar denenecek.",
                    attempt, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private async Task<IReadOnlyList<(float[] Embedding, int Index)>> EmbedBatchAsync(
        string[] batch, CancellationToken cancellationToken)
    {
        var request = new VoyageEmbeddingRequest(_options.Model, batch, EmbeddingDimensions.Standard, Truncation: true);

        using var response = await _httpClient.PostAsJsonAsync("embeddings", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new VoyageEmbeddingException(
                $"Voyage AI embedding isteği {(int)response.StatusCode} döndürdü: {body}", response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<VoyageEmbeddingResponse>(cancellationToken)
            ?? throw new VoyageEmbeddingException("Voyage AI boş bir yanıt gövdesi döndürdü.", response.StatusCode);

        return payload.Data.Select(datum =>
        {
            // Fail-fast: yanlış boyutta bir vektörü sessizce kaydetmek,
            // Ders #15'teki (sıfır-vektör -> NaN -> tüm arama isteği çöktü)
            // sınıftan bir hatayı ileriye ötelemek olurdu - burada erken
            // yakalamak, sorunu pgvector'a ulaşmadan tespit ediyor.
            if (datum.Embedding.Length != EmbeddingDimensions.Standard)
                throw new VoyageEmbeddingException(
                    $"Voyage AI {datum.Embedding.Length} boyutunda bir vektör döndürdü, " +
                    $"beklenen {EmbeddingDimensions.Standard}.", response.StatusCode);

            return (datum.Embedding, datum.Index);
        }).ToList();
    }

    private static bool IsTransient(Exception ex, CancellationToken cancellationToken)
    {
        // Çağıran taraf isteği zaten iptal ettiyse (kendi CancellationToken'ı
        // tetiklendiyse) yeniden denemek anlamsız - onu olduğu gibi yukarı
        // fırlatmalıyız, transient bir hata gibi ele almamalıyız.
        if (cancellationToken.IsCancellationRequested)
            return false;

        return ex switch
        {
            VoyageEmbeddingException voyageEx => voyageEx.StatusCode is HttpStatusCode.TooManyRequests
                or >= HttpStatusCode.InternalServerError,
            HttpRequestException => true,
            TaskCanceledException => true, // HttpClient.Timeout de bu türü fırlatıyor
            _ => false,
        };
    }
}
