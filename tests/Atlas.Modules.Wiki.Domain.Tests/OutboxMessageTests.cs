using Atlas.Modules.Wiki.Domain.Entities;

namespace Atlas.Modules.Wiki.Domain.Tests;

public class OutboxMessageTests
{
    [Fact]
    public void GecerliBilgilerle_MesajBasariylaOlusur()
    {
        var message = OutboxMessage.Create("Atlas.Shared.Contracts.WikiPageCreatedEvent", "{\"PageId\":\"...\"}");

        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal("Atlas.Shared.Contracts.WikiPageCreatedEvent", message.EventType);
        Assert.Null(message.ProcessedAtUtc);
        Assert.Equal(0, message.Attempts);
    }

    [Fact]
    public void OlusturulanMesaj_SimdikiZamanlaDamgalanir()
    {
        var before = DateTime.UtcNow;

        var message = OutboxMessage.Create("SomeEvent", "{}");

        var after = DateTime.UtcNow;
        Assert.InRange(message.OccurredAtUtc, before, after);
    }

    [Fact]
    public void BosEventTypeIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() => OutboxMessage.Create("   ", "{}"));
    }

    [Fact]
    public void BosPayloadIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() => OutboxMessage.Create("SomeEvent", "   "));
    }

    [Fact]
    public void MarkProcessed_ProcessedAtUtcyiDoldurur()
    {
        var message = OutboxMessage.Create("SomeEvent", "{}");

        message.MarkProcessed();

        Assert.NotNull(message.ProcessedAtUtc);
    }

    [Fact]
    public void MarkFailed_DenemeSayisiniArtirirVeSonHatayiKaydeder()
    {
        var message = OutboxMessage.Create("SomeEvent", "{}");

        message.MarkFailed("Bağlantı zaman aşımına uğradı.");
        message.MarkFailed("Yine başarısız oldu.");

        Assert.Equal(2, message.Attempts);
        Assert.Equal("Yine başarısız oldu.", message.LastError);
        Assert.Null(message.ProcessedAtUtc);
    }

    [Fact]
    public void MaxAttemptsAltinda_DeadLetterDegildir()
    {
        var message = OutboxMessage.Create("SomeEvent", "{}");

        for (var i = 0; i < OutboxMessage.MaxAttempts - 1; i++)
            message.MarkFailed("hata");

        Assert.False(message.IsDeadLettered);
    }

    [Fact]
    public void MaxAttemptsUlasinca_DeadLetterOlur()
    {
        var message = OutboxMessage.Create("SomeEvent", "{}");

        for (var i = 0; i < OutboxMessage.MaxAttempts; i++)
            message.MarkFailed("hata");

        Assert.True(message.IsDeadLettered);
    }

    [Fact]
    public void MaxAttemptsUlassaBile_IslenmisseDeadLetterDegildir()
    {
        var message = OutboxMessage.Create("SomeEvent", "{}");

        for (var i = 0; i < OutboxMessage.MaxAttempts; i++)
            message.MarkFailed("hata");
        message.MarkProcessed();

        Assert.False(message.IsDeadLettered);
    }
}
