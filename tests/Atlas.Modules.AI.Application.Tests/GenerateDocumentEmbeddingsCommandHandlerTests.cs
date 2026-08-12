using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Modules.AI.Application.Documents.Commands;
using Atlas.Modules.AI.Application.Tests.Fakes;
using Atlas.Modules.AI.Domain.Entities;

namespace Atlas.Modules.AI.Application.Tests;

public class GenerateDocumentEmbeddingsCommandHandlerTests
{
    [Fact]
    public async Task Handle_OnceEskiEmbeddingleriSiler_SonraChunkBasinaBirEmbeddingEkler()
    {
        var repository = new FakeDocumentEmbeddingRepository();
        var handler = new GenerateDocumentEmbeddingsCommandHandler(new FakeEmbeddingServiceForTests(), repository);
        var documentId = Guid.NewGuid();

        await handler.Handle(
            new GenerateDocumentEmbeddingsCommand(
                documentId, ["birinci parça", "ikinci parça"], "Test Belgesi", "IT", "Public"),
            CancellationToken.None);

        // ReprocessDocumentCommand ile bu Command ikinci kez tetiklenebiliyor
        // (bkz. Handler'daki idempotency notu) - silme HER ZAMAN önce çağrılmalı.
        Assert.Contains(documentId, repository.DeletedDocumentIds);

        Assert.Equal(2, repository.AddedEmbeddings.Count);
        Assert.All(repository.AddedEmbeddings, e => Assert.Equal(documentId, e.DocumentId));
        Assert.Contains(repository.AddedEmbeddings, e => e.ChunkIndex == 0 && e.ChunkText == "birinci parça");
        Assert.Contains(repository.AddedEmbeddings, e => e.ChunkIndex == 1 && e.ChunkText == "ikinci parça");
    }

    [Fact]
    public async Task Handle_SifirVektorluChunk_Kaydedilmez()
    {
        var repository = new FakeDocumentEmbeddingRepository();
        var handler = new GenerateDocumentEmbeddingsCommandHandler(new ZeroVectorForFirstChunkEmbeddingService(), repository);
        var documentId = Guid.NewGuid();

        await handler.Handle(
            new GenerateDocumentEmbeddingsCommand(
                documentId, ["????????", "anlamli parça"], "Test Belgesi", "IT", "Public"),
            CancellationToken.None);

        // Ders #15'teki AYNI savunma: sıfır vektör üreten (anlamsız) chunk
        // kaydedilmiyor, aksi halde pgvector'ın cosine distance'ı NaN üretip
        // tüm arama isteğini çökertebilirdi.
        var added = Assert.Single(repository.AddedEmbeddings);
        Assert.Equal("anlamli parça", added.ChunkText);
    }

    [Fact]
    public async Task Handle_BosChunkListesi_HicEmbeddingEklemezAmaEskileriYineDeSiler()
    {
        var repository = new FakeDocumentEmbeddingRepository();
        var handler = new GenerateDocumentEmbeddingsCommandHandler(new FakeEmbeddingServiceForTests(), repository);
        var documentId = Guid.NewGuid();

        await handler.Handle(
            new GenerateDocumentEmbeddingsCommand(documentId, [], "Test Belgesi", "IT", "Public"),
            CancellationToken.None);

        Assert.Contains(documentId, repository.DeletedDocumentIds);
        Assert.Empty(repository.AddedEmbeddings);
    }

    // İlk chunk için sıfır, diğerleri için sabit-1 vektör dönen - gerçek
    // FakeEmbeddingService'in "anlamsız girdi -> sıfır vektör" davranışını
    // (bkz. Ders #15) izole test edebilmek için.
    private class ZeroVectorForFirstChunkEmbeddingService : IEmbeddingService
    {
        public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<float[]> result = texts
                .Select(t => t == "????????"
                    ? new float[EmbeddingDimensions.Standard]
                    : Enumerable.Repeat(1f, EmbeddingDimensions.Standard).ToArray())
                .ToList();
            return Task.FromResult(result);
        }
    }
}
