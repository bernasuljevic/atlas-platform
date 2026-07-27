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
        var embeddings = chunks
            .Select((chunkText, index) =>
                WikiPageEmbedding.Create(
                    request.WikiPageId, index, chunkText, request.Title,
                    request.DepartmentName, request.Visibility, vectors[index]))
            .ToList();

        await _repository.AddRangeAsync(embeddings, cancellationToken);
    }
}
