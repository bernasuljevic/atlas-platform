using Atlas.Shared.Kernel.Entities;
using Pgvector;

namespace Atlas.Modules.AI.Domain.Entities;

// AI, Wiki'nin WikiPage entity'sini hiç tanımıyor - sadece hangi wiki sayfasına
// ait olduğunu WikiPageId (Guid) ile tutuyor. Modüller arası kural burada da geçerli:
// AI, Wiki'nin Domain/Infrastructure'ına referans vermez.
public class WikiPageEmbedding : Entity<Guid>
{
    public Guid WikiPageId { get; private set; }

    // Vector, Pgvector paketinin sağladığı value type - Infrastructure'daki
    // "vector(1024)" sütun tipiyle dolaysız eşleşiyor.
    public Vector Embedding { get; private set; } = default!;

    public DateTime CreatedAtUtc { get; private set; }

    private WikiPageEmbedding() { }

    private WikiPageEmbedding(Guid id, Guid wikiPageId, Vector embedding, DateTime createdAtUtc) : base(id)
    {
        WikiPageId = wikiPageId;
        Embedding = embedding;
        CreatedAtUtc = createdAtUtc;
    }

    // Factory metodu float[] alıyor - dışarıdan (ileride embedding/LLM katmanından)
    // çağıranlar Pgvector'ı hiç bilmek zorunda kalmıyor, sadece ham sayı dizisi veriyorlar.
    public static WikiPageEmbedding Create(Guid wikiPageId, float[] embedding)
    {
        if (wikiPageId == Guid.Empty)
            throw new ArgumentException("WikiPageId boş olamaz.", nameof(wikiPageId));

        if (embedding is null || embedding.Length == 0)
            throw new ArgumentException("Embedding boş olamaz.", nameof(embedding));

        return new WikiPageEmbedding(Guid.NewGuid(), wikiPageId, new Vector(embedding), DateTime.UtcNow);
    }
}
