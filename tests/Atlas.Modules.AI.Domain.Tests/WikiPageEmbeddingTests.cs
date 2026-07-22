using Atlas.Modules.AI.Domain.Entities;
using Xunit;

namespace Atlas.Modules.AI.Domain.Tests;

public class WikiPageEmbeddingTests
{
    [Fact]
    public void GecerliBilgilerle_EmbeddingBasariylaOlusur()
    {
        var wikiPageId = Guid.NewGuid();
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        var result = WikiPageEmbedding.Create(wikiPageId, embedding);

        Assert.Equal(wikiPageId, result.WikiPageId);
        Assert.Equal(embedding, result.Embedding.ToArray());
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public void OlusturulanEmbedding_SimdikiZamanlaDamgalanir()
    {
        var before = DateTime.UtcNow;

        var result = WikiPageEmbedding.Create(Guid.NewGuid(), new float[] { 0.1f });

        var after = DateTime.UtcNow;
        Assert.InRange(result.CreatedAtUtc, before, after);
    }

    [Fact]
    public void BosWikiPageIdIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() =>
            WikiPageEmbedding.Create(Guid.Empty, new float[] { 0.1f, 0.2f }));
    }

    [Fact]
    public void BosEmbeddingDizisiIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() =>
            WikiPageEmbedding.Create(Guid.NewGuid(), Array.Empty<float>()));
    }

    [Fact]
    public void NullEmbeddingIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() =>
            WikiPageEmbedding.Create(Guid.NewGuid(), null!));
    }
}
