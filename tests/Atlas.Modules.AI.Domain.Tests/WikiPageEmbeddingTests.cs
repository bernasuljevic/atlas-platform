using Atlas.Modules.AI.Domain.Entities;
using Xunit;

namespace Atlas.Modules.AI.Domain.Tests;

public class WikiPageEmbeddingTests
{
    // Gerçek boyutta (1024) bir vektörü elle 1024 kere yazmamak için - değerler
    // önemli değil, sadece UZUNLUK kontrol ediliyor bu testlerde.
    private static float[] ValidEmbedding() => new float[WikiPageEmbedding.EmbeddingDimension];

    [Fact]
    public void GecerliBilgilerle_EmbeddingBasariylaOlusur()
    {
        var wikiPageId = Guid.NewGuid();
        var embedding = ValidEmbedding();

        var result = WikiPageEmbedding.Create(
            wikiPageId, chunkIndex: 0, chunkText: "Giriş bölümü", title: "Başlık",
            departmentName: "IK", visibility: "Public", embedding);

        Assert.Equal(wikiPageId, result.WikiPageId);
        Assert.Equal(0, result.ChunkIndex);
        Assert.Equal("Giriş bölümü", result.ChunkText);
        Assert.Equal("Başlık", result.Title);
        Assert.Equal("IK", result.DepartmentName);
        Assert.Equal("Public", result.Visibility);
        Assert.Equal(embedding, result.Embedding.ToArray());
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public void OlusturulanEmbedding_SimdikiZamanlaDamgalanir()
    {
        var before = DateTime.UtcNow;

        var result = WikiPageEmbedding.Create(
            Guid.NewGuid(), chunkIndex: 0, chunkText: "x", title: "Başlık",
            departmentName: "IK", visibility: "Public", ValidEmbedding());

        var after = DateTime.UtcNow;
        Assert.InRange(result.CreatedAtUtc, before, after);
    }

    [Fact]
    public void BosWikiPageIdIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() =>
            WikiPageEmbedding.Create(
                Guid.Empty, chunkIndex: 0, chunkText: "x", title: "Başlık",
                departmentName: "IK", visibility: "Public", ValidEmbedding()));
    }

    [Fact]
    public void NegatifChunkIndexIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() =>
            WikiPageEmbedding.Create(
                Guid.NewGuid(), chunkIndex: -1, chunkText: "x", title: "Başlık",
                departmentName: "IK", visibility: "Public", ValidEmbedding()));
    }

    [Fact]
    public void BosChunkTextIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() =>
            WikiPageEmbedding.Create(
                Guid.NewGuid(), chunkIndex: 0, chunkText: "   ", title: "Başlık",
                departmentName: "IK", visibility: "Public", ValidEmbedding()));
    }

    [Fact]
    public void BosTitleIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() =>
            WikiPageEmbedding.Create(
                Guid.NewGuid(), chunkIndex: 0, chunkText: "x", title: "   ",
                departmentName: "IK", visibility: "Public", ValidEmbedding()));
    }

    [Fact]
    public void BosDepartmentNameIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() =>
            WikiPageEmbedding.Create(
                Guid.NewGuid(), chunkIndex: 0, chunkText: "x", title: "Başlık",
                departmentName: "   ", visibility: "Public", ValidEmbedding()));
    }

    [Fact]
    public void BosVisibilityIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() =>
            WikiPageEmbedding.Create(
                Guid.NewGuid(), chunkIndex: 0, chunkText: "x", title: "Başlık",
                departmentName: "IK", visibility: "   ", ValidEmbedding()));
    }

    [Fact]
    public void BosEmbeddingDizisiIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() =>
            WikiPageEmbedding.Create(
                Guid.NewGuid(), chunkIndex: 0, chunkText: "x", title: "Başlık",
                departmentName: "IK", visibility: "Public", Array.Empty<float>()));
    }

    [Fact]
    public void NullEmbeddingIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() =>
            WikiPageEmbedding.Create(
                Guid.NewGuid(), chunkIndex: 0, chunkText: "x", title: "Başlık",
                departmentName: "IK", visibility: "Public", null!));
    }

    [Fact]
    public void YanlisBoyutluEmbeddingIle_ArgumentExceptionFirlatilir()
    {
        // Veritabanı sütunu "vector(1024)" olarak sabit - 1024'ten farklı bir
        // boyut Postgres'e ulaşmadan, burada (fail fast) yakalanmalı.
        var yanlisBoyutlu = new float[] { 0.1f, 0.2f, 0.3f };

        Assert.Throws<ArgumentException>(() =>
            WikiPageEmbedding.Create(
                Guid.NewGuid(), chunkIndex: 0, chunkText: "x", title: "Başlık",
                departmentName: "IK", visibility: "Public", yanlisBoyutlu));
    }
}
