using Atlas.Shared.Kernel.Entities;
using Pgvector;

namespace Atlas.Modules.AI.Domain.Entities;

// AI, Wiki'nin WikiPage entity'sini hiç tanımıyor - sadece hangi wiki sayfasına
// ait olduğunu WikiPageId (Guid) ile tutuyor. Modüller arası kural burada da geçerli:
// AI, Wiki'nin Domain/Infrastructure'ına referans vermez.
public class WikiPageEmbedding : Entity<Guid>
{
    // Bu sayı, Infrastructure'daki "vector(...)" sütun tipiyle EŞLEŞMEK ZORUNDA
    // (şu an Voyage AI'a göre 1024). Sabiti burada, TEK bir yerde tanımlıyoruz -
    // hem bu entity'nin validasyonu hem de EF Core configuration'ı buradan okuyor.
    // Aksi halde "1024" sayısını iki ayrı yerde elle senkron tutmak zorunda
    // kalırdık ve bir gün biri güncellenip diğeri unutulurdu.
    public const int EmbeddingDimension = 1024;

    public Guid WikiPageId { get; private set; }

    // Bir wiki sayfası birden fazla parçaya (chunk) bölünüyor - bu yüzden
    // WikiPageId üzerinde bilinçli olarak unique constraint YOK: aynı sayfaya
    // ait birden fazla satır olacak, her satır TEK bir chunk'ı temsil ediyor.
    // ChunkIndex, o chunk'ın sayfa içindeki sırasını tutuyor - veritabanı
    // satırların kaydedilme sırasını hatırlamadığı için (SQL'de sıra garantisi
    // yoktur, sadece ORDER BY ile istenir) bu bilgiyi ayrı bir sütunda
    // saklamazsak, sayfayı yeniden "birleştirmek" ya da sıralı göstermek
    // imkansız hale gelir.
    public int ChunkIndex { get; private set; }

    public string ChunkText { get; private set; } = string.Empty;

    // Vector, Pgvector paketinin sağladığı value type - Infrastructure'daki
    // "vector(1024)" sütun tipiyle dolaysız eşleşiyor.
    public Vector Embedding { get; private set; } = default!;

    public DateTime CreatedAtUtc { get; private set; }

    private WikiPageEmbedding() { }

    private WikiPageEmbedding(
        Guid id, Guid wikiPageId, int chunkIndex, string chunkText, Vector embedding, DateTime createdAtUtc)
        : base(id)
    {
        WikiPageId = wikiPageId;
        ChunkIndex = chunkIndex;
        ChunkText = chunkText;
        Embedding = embedding;
        CreatedAtUtc = createdAtUtc;
    }

    // Factory metodu float[] alıyor - dışarıdan (embedding/LLM katmanından)
    // çağıranlar Pgvector'ı hiç bilmek zorunda kalmıyor, sadece ham sayı dizisi veriyorlar.
    public static WikiPageEmbedding Create(Guid wikiPageId, int chunkIndex, string chunkText, float[] embedding)
    {
        if (wikiPageId == Guid.Empty)
            throw new ArgumentException("WikiPageId boş olamaz.", nameof(wikiPageId));

        if (chunkIndex < 0)
            throw new ArgumentException("ChunkIndex negatif olamaz.", nameof(chunkIndex));

        if (string.IsNullOrWhiteSpace(chunkText))
            throw new ArgumentException("ChunkText boş olamaz.", nameof(chunkText));

        if (embedding is null || embedding.Length == 0)
            throw new ArgumentException("Embedding boş olamaz.", nameof(embedding));

        // Fail fast: yanlış boyutlu bir embedding'i veritabanına göndermeden ÖNCE
        // burada yakalıyoruz. Aksi halde Postgres'in "vector(1024)" sütun tipi bunu
        // zaten reddedecekti, ama o hata mesajı kriptik bir Npgsql exception olurdu -
        // burada anlaşılır bir Domain hatası fırlatıyoruz.
        if (embedding.Length != EmbeddingDimension)
            throw new ArgumentException(
                $"Embedding {EmbeddingDimension} boyutunda olmalı, {embedding.Length} geldi.",
                nameof(embedding));

        return new WikiPageEmbedding(Guid.NewGuid(), wikiPageId, chunkIndex, chunkText, new Vector(embedding), DateTime.UtcNow);
    }
}
