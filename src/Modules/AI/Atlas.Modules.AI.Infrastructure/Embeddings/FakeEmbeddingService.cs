using System.Security.Cryptography;
using System.Text;
using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Modules.AI.Domain.Entities;

namespace Atlas.Modules.AI.Infrastructure.Embeddings;

/// <summary>
/// Gerçek bir embedding modeli (Voyage AI, OpenAI vb.) gelene kadar IEmbeddingService'i
/// karşılayan geçici bir implementasyon - Pbkdf2PasswordHasher'ın IPasswordHasher'ı
/// karşılaması gibi aynı desen.
///
/// Rastgele/anlamsız bir vektör üretmek yerine "feature hashing" (hashing trick)
/// denen basit bir teknik kullanıyoruz: metindeki her kelimeyi 1024 "kova"dan
/// birine hash'leyip o kovayı 1 artırıyoruz, sonra vektörü normalize ediyoruz.
/// Sonuç: gerçek bir ML modeli kadar iyi değil, ama ORTAK KELİMESİ ÇOK OLAN iki
/// metin, matematiksel olarak da birbirine YAKIN vektörler üretir - yani arama
/// özelliğini şimdiden GERÇEKÇİ bir şekilde test edebiliyoruz, tamamen rastgele
/// sayılarla değil.
/// </summary>
public class FakeEmbeddingService : IEmbeddingService
{
    private static readonly char[] WordSeparators =
        { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}', '-', '/' };

    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        // Sıra garantisi burada doğal olarak korunuyor: Select, LINQ'de girdi
        // sırasını değiştirmez - IEmbeddingService'in sözleşmesi (çıktı[i] = girdi[i])
        // bu implementasyonda otomatik sağlanıyor.
        var vectors = texts.Select(Embed).ToList();
        return Task.FromResult<IReadOnlyList<float[]>>(vectors);
    }

    private static float[] Embed(string text)
    {
        var vector = new float[WikiPageEmbedding.EmbeddingDimension];

        foreach (var word in Tokenize(text))
        {
            var bucket = HashToBucket(word, vector.Length);
            vector[bucket] += 1f;
        }

        Normalize(vector);
        return vector;
    }

    private static IEnumerable<string> Tokenize(string text) =>
        text.ToLowerInvariant().Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);

    private static int HashToBucket(string word, int bucketCount)
    {
        // string.GetHashCode() KULLANMIYORUZ: .NET, güvenlik amacıyla (hash
        // flooding saldırılarına karşı) her PROCESS başlatıldığında farklı bir
        // hash tuzu (salt) kullanır - yani aynı metin, uygulamayı yeniden
        // başlattığında FARKLI bir sayı üretir. Bu da IEmbeddingService'in
        // "aynı metin her zaman aynı vektör" sözleşmesini bozardı. MD5 gibi
        // kriptografik bir hash fonksiyonu ise girdiye göre HER ZAMAN aynı
        // çıktıyı üretir - süreç, makine, zaman fark etmez.
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(word));
        var value = BitConverter.ToUInt32(hashBytes, 0);
        return (int)(value % (uint)bucketCount);
    }

    private static void Normalize(float[] vector)
    {
        // L2 normalizasyon: vektörü "birim uzunluğa" indiriyoruz. Bunu yapmazsak,
        // sadece daha ÇOK kelime içeren (daha uzun) bir chunk, kelime örtüşmesi
        // aynı olsa bile matematiksel olarak "daha büyük" bir vektör üretir ve
        // benzerlik karşılaştırmasını (cosine similarity) bozar.
        var magnitude = MathF.Sqrt(vector.Sum(v => v * v));

        if (magnitude <= 0f)
            return;

        for (var i = 0; i < vector.Length; i++)
            vector[i] /= magnitude;
    }
}
