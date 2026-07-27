using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Modules.AI.Domain.Chunking;
using Atlas.Modules.AI.Domain.Entities;
using MediatR;

namespace Atlas.Modules.AI.Application.WikiPages.Commands;

public class GenerateWikiPageEmbeddingsCommandHandler : IRequestHandler<GenerateWikiPageEmbeddingsCommand>
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IWikiPageEmbeddingRepository _repository;

    public GenerateWikiPageEmbeddingsCommandHandler(
        IEmbeddingService embeddingService, IWikiPageEmbeddingRepository repository)
    {
        _embeddingService = embeddingService;
        _repository = repository;
    }

    public async Task Handle(GenerateWikiPageEmbeddingsCommand request, CancellationToken cancellationToken)
    {
        // Önce bu sayfaya ait eski chunk'ları temizliyoruz - bu Command normalde
        // sadece BİR KEZ (sayfa oluşunca) çalışır, ama artık ikinci bir tetikleyicisi
        // de var: ReindexWikiPagesCommand (Wiki.Application), embedding'leri toplu
        // yeniden üretmek için AYNI event'i (WikiPageCreatedEvent) tekrar yayınlıyor.
        // Bu satır olmasaydı, bir sayfayı reindex etmek chunk'ları İKİ KERE (mükerrer)
        // eklerdi - idempotent olmayan bir davranış.
        await _repository.DeleteByWikiPageIdAsync(request.WikiPageId, cancellationToken);

        // 1) Böl: Gün 1-2'de tasarladığımız TextChunker, uzun sayfayı üst üste
        // binen parçalara ayırıyor.
        var chunks = TextChunker.Chunk(request.Content);

        // 2) Vektörleştir: TÜM chunk'ları TEK bir batch çağrıda gönderiyoruz -
        // IEmbeddingService'in sözleşmesi gereği, dönen liste chunks ile AYNI
        // sırada geliyor (bkz. interface'teki XML yorumu).
        var vectors = await _embeddingService.EmbedAsync(chunks, cancellationToken);

        // 3) Kaydet: her chunk'ı kendi sırasıyla (ChunkIndex) bir WikiPageEmbedding
        // satırına çeviriyoruz - WikiPageEmbedding.Create zaten boyut/boşluk
        // validasyonunu kendisi yapıyor (Gün 1'deki fail-fast kontrolü).
        //
        // BULUNAN GERÇEK BUG: bir chunk hiç tanınabilir kelime içermiyorsa (örn.
        // sadece "????????" gibi noktalama işaretlerinden oluşuyorsa),
        // FakeEmbeddingService'in Normalize() metodu 0'a bölmeyi önlemek için
        // vektörü normalize ETMEDEN (tamamen sıfır olarak) döndürüyor - kendi
        // içinde güvenli, ama bu sıfır vektör Postgres'e kaydedilince, pgvector
        // sonradan onunla cosine distance hesaplarken (|sıfır vektör|=0, 0'a
        // bölme) NaN üretiyor - bu da arama sonucunu JSON'a yazarken tüm isteği
        // çökertiyor (canlı doğrulandı). Anlamsız/sıfır vektörlü chunk'ları
        // baştan HİÇ kaydetmiyoruz.
        var embeddings = chunks
            .Select((chunkText, index) => (chunkText, index, vector: vectors[index]))
            .Where(x => x.vector.Any(component => component != 0f))
            .Select(x => WikiPageEmbedding.Create(
                request.WikiPageId, x.index, x.chunkText, request.Title,
                request.DepartmentName, request.Visibility, x.vector))
            .ToList();

        await _repository.AddRangeAsync(embeddings, cancellationToken);
    }
}
