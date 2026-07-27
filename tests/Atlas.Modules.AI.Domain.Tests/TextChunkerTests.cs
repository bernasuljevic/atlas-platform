using Atlas.Modules.AI.Domain.Chunking;
using Xunit;

namespace Atlas.Modules.AI.Domain.Tests;

public class TextChunkerTests
{
    [Fact]
    public void IcerikChunkSizedanKisaysa_TekBirChunkDoner()
    {
        var content = "Kısa bir metin.";

        var chunks = TextChunker.Chunk(content, chunkSize: 500, overlap: 50);

        Assert.Single(chunks);
        Assert.Equal(content, chunks[0]);
    }

    [Fact]
    public void IcerikChunkSizedanUzunsa_BirdenFazlaChunkUretilir()
    {
        var content = new string('a', 1200);

        var chunks = TextChunker.Chunk(content, chunkSize: 500, overlap: 50);

        // step = 500 - 50 = 450 -> [0-500), [450-950), [900-1200) => 3 chunk
        Assert.Equal(3, chunks.Count);
        Assert.Equal(500, chunks[0].Length);
        Assert.Equal(500, chunks[1].Length);
        Assert.Equal(300, chunks[2].Length);
    }

    [Fact]
    public void ArdisikChunklar_OverlapKadarOrtakIcerikTasir()
    {
        var content = new string('a', 1200);

        var chunks = TextChunker.Chunk(content, chunkSize: 500, overlap: 50);

        // 0. chunk [0,500), 1. chunk [450,950) -> ortak kısım [450,500) = 50 karakter.
        var ilkChunkinSonu = chunks[0][^50..];
        var ikinciChunkinBasi = chunks[1][..50];
        Assert.Equal(ilkChunkinSonu, ikinciChunkinBasi);
    }

    [Fact]
    public void TumChunklarBirlestiginde_IcerigiTamOlarakKapsar()
    {
        // Chunk'ların üst üste binmesi bilgi kaybını önlüyor - bunu somut olarak
        // doğrulamak için içeriğin HER karakterinin en az bir chunk'ta yer aldığını
        // kontrol ediyoruz (basit bir işaretleme/kapsama testi).
        var content = new string('x', 1000);
        var covered = new bool[content.Length];

        var chunks = TextChunker.Chunk(content, chunkSize: 300, overlap: 40);

        var position = 0;
        var step = 300 - 40;
        foreach (var chunk in chunks)
        {
            for (var i = 0; i < chunk.Length; i++)
                covered[position + i] = true;
            position += step;
        }

        Assert.All(covered, isCovered => Assert.True(isCovered));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BosIcerikIle_ArgumentExceptionFirlatilir(string? content)
    {
        Assert.Throws<ArgumentException>(() => TextChunker.Chunk(content!));
    }

    [Fact]
    public void SifirVeyaNegatifChunkSizeIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() => TextChunker.Chunk("içerik", chunkSize: 0));
        Assert.Throws<ArgumentException>(() => TextChunker.Chunk("içerik", chunkSize: -10));
    }

    [Fact]
    public void NegatifOverlapIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() => TextChunker.Chunk("içerik", chunkSize: 100, overlap: -1));
    }

    [Fact]
    public void OverlapChunkSizeyeEsitVeyaBuyukse_ArgumentExceptionFirlatilir()
    {
        // Bu, sonsuz döngü koruması - overlap >= chunkSize olursa kayan pencere
        // hiç ilerlemez. Testin kendisi de sonsuz döngüye düşmemeli: eğer
        // guard clause çalışmazsa bu test asla bitmez ve zaman aşımıyla patlar,
        // bu yüzden guard'ın "girişte" attığını doğruluyoruz.
        Assert.Throws<ArgumentException>(() => TextChunker.Chunk("içerik", chunkSize: 100, overlap: 100));
        Assert.Throws<ArgumentException>(() => TextChunker.Chunk("içerik", chunkSize: 100, overlap: 150));
    }
}
