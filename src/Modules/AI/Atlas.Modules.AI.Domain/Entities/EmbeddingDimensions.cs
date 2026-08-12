namespace Atlas.Modules.AI.Domain.Entities;

/// <summary>
/// Embedding sağlayıcısının ürettiği vektör boyutu - hem WikiPageEmbedding hem
/// (P5'ten itibaren) DocumentEmbedding kullanıyor. "1024" sayısı burada TEK
/// yerde tanımlı - iki entity'nin de kendi EmbeddingDimension sabiti buradan
/// türüyor, aksi halde biri güncellenip diğeri unutulabilirdi (WikiPageEmbedding'in
/// orijinal yorumundaki AYNI gerekçe, artık iki tüketicisi olduğu için ayrı bir
/// sınıfa çıkarıldı).
/// </summary>
public static class EmbeddingDimensions
{
    public const int Standard = 1024;
}
