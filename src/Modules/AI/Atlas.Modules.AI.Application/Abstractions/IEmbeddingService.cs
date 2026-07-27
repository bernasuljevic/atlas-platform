namespace Atlas.Modules.AI.Application.Abstractions;

/// <summary>
/// AI.Infrastructure'da implemente edilecek - önce sahte/lokal bir üretici,
/// ileride gerçek bir sağlayıcı (Voyage AI, OpenAI vb.) ile. Application
/// katmanı HANGİSİNİN kullanıldığını hiç bilmiyor; tıpkı IPasswordHasher'ın
/// Pbkdf2PasswordHasher'a bağlı olması gibi (Auth modülündeki aynı desen).
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Verilen metinleri toplu (batch) olarak vektörleştirir - gerçek embedding
    /// API'lerinin (OpenAI, Voyage) çoğu, tek tek çağrı yerine batch çağrıyı
    /// destekliyor ve daha verimli.
    ///
    /// SÖZLEŞME: Dönen listenin i'inci elemanı, "texts" parametresinin i'inci
    /// elemanına karşılık gelir - sıra KORUNUR. Bu interface'i implemente eden
    /// her sınıf bu sözleşmeyi bozmamalı; aksi halde çağıran taraf yanlış
    /// vektörü yanlış metne (chunk'a) eşleştirip kaydeder.
    ///
    /// Her vektörün uzunluğu <see cref="Atlas.Modules.AI.Domain.Entities.WikiPageEmbedding.EmbeddingDimension"/>'a
    /// EŞİT olmalı - veritabanı sütunu bu boyutu sabit kabul ediyor.
    /// </summary>
    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
