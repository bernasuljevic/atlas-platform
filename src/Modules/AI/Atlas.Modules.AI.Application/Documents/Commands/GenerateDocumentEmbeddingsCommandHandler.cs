using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Modules.AI.Domain.Entities;
using MediatR;

namespace Atlas.Modules.AI.Application.Documents.Commands;

public class GenerateDocumentEmbeddingsCommandHandler : IRequestHandler<GenerateDocumentEmbeddingsCommand>
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentEmbeddingRepository _repository;

    public GenerateDocumentEmbeddingsCommandHandler(
        IEmbeddingService embeddingService, IDocumentEmbeddingRepository repository)
    {
        _embeddingService = embeddingService;
        _repository = repository;
    }

    public async Task Handle(GenerateDocumentEmbeddingsCommand request, CancellationToken cancellationToken)
    {
        // GenerateWikiPageEmbeddingsCommandHandler'daki AYNI idempotency gerekçesi -
        // ama tetikleyicisi burada ReindexWikiPagesCommand DEĞİL, ReprocessDocumentCommand
        // (Documents.Application, Gün 5): bir belge yeniden işlenince
        // DocumentChunksReadyEvent İKİNCİ kez yayınlanır, bu satır olmasaydı
        // eski chunk'lar silinmeden yenileri eklenir, aynı belgenin embedding'leri
        // mükerrerleşirdi.
        await _repository.DeleteByDocumentIdAsync(request.DocumentId, cancellationToken);

        if (request.ChunkTexts.Count == 0)
            return;

        // Chunking ZATEN yapılmış geliyor (bkz. Command'daki not) - burada
        // sadece vektörleştirme + kaydetme var, TextChunker çağrısı YOK.
        var vectors = await _embeddingService.EmbedAsync(request.ChunkTexts, cancellationToken);

        // GenerateWikiPageEmbeddingsCommandHandler'daki AYNI sıfır-vektör
        // savunması (Ders #15) - anlamsız/boş bir chunk kaydedilmiyor.
        var embeddings = request.ChunkTexts
            .Select((chunkText, index) => (chunkText, index, vector: vectors[index]))
            .Where(x => x.vector.Any(component => component != 0f))
            .Select(x => DocumentEmbedding.Create(
                request.DocumentId, x.index, x.chunkText, request.Title,
                request.DepartmentName, request.Visibility, x.vector))
            .ToList();

        await _repository.AddRangeAsync(embeddings, cancellationToken);
    }
}
