using Atlas.Modules.AI.Application.Abstractions;

namespace Atlas.Modules.AI.Application.Tests.Fakes;

/// <summary>
/// Gerçek FakeEmbeddingService (Infrastructure'daki) ile karıştırılmasın diye
/// "Test" son eki taşıyor - bu, Handler'ı embedding matematiğinden tamamen
/// izole etmek için var. Hangi sorgu metni gönderilirse gönderilsin sabit bir
/// vektör dönüyor; testler zaten benzerlik SIRALAMASINI repository seviyesinde
/// (FakeWikiPageEmbeddingRepository'nin döndürdüğü hazır mesafelerle) kontrol
/// ediyor, gerçek vektör matematiğine ihtiyaç yok.
/// </summary>
public class FakeEmbeddingServiceForTests : IEmbeddingService
{
    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<float[]> result = texts.Select(_ => new float[] { 1f }).ToList();
        return Task.FromResult(result);
    }
}
