using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Modules.AI.Domain.Entities;

namespace Atlas.Modules.AI.Application.Tests.Fakes;

/// <summary>
/// Gerçek FakeEmbeddingService (Infrastructure'daki) ile karıştırılmasın diye
/// "Test" son eki taşıyor - bu, Handler'ı embedding matematiğinden tamamen
/// izole etmek için var. Hangi sorgu metni gönderilirse gönderilsin sabit bir
/// vektör dönüyor; testler zaten benzerlik SIRALAMASINI repository seviyesinde
/// (FakeWikiPageEmbeddingRepository'nin döndürdüğü hazır mesafelerle) kontrol
/// ediyor, gerçek vektör matematiğine ihtiyaç yok.
///
/// Vektör boyutu EmbeddingDimensions.Standard (1024) - GenerateDocumentEmbeddingsCommandHandler
/// (P5 Gün 2) gibi DocumentEmbedding.Create/WikiPageEmbedding.Create'i GERÇEKTEN
/// çağıran Handler'lar yanlış boyutlu bir vektörde ArgumentException fırlatır;
/// eskiden burada boyutu 1 olan bir vektör dönülüyordu (SearchWikiPagesByMeaningQueryHandlerTests'te
/// sorun çıkmıyordu çünkü o test akışı hiç Create() çağırmıyor) - artık her iki
/// kullanım için de doğru.
/// </summary>
public class FakeEmbeddingServiceForTests : IEmbeddingService
{
    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<float[]> result = texts
            .Select(_ => Enumerable.Repeat(1f, EmbeddingDimensions.Standard).ToArray())
            .ToList();
        return Task.FromResult(result);
    }
}
