using Atlas.Modules.Audit.Domain.Entities;

namespace Atlas.Modules.Audit.Domain.Tests;

public class AuditLogEntryTests
{
    [Fact]
    public void GecerliBilgilerle_SatirBasariylaOlusur()
    {
        var userId = Guid.NewGuid();

        var entry = AuditLogEntry.Create(userId, "admin@atlas.local", "WikiPage.Created", "resource-1");

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(userId, entry.UserId);
        Assert.Equal("admin@atlas.local", entry.UserEmail);
        Assert.Equal("WikiPage.Created", entry.Action);
        Assert.Equal("resource-1", entry.ResourceId);
    }

    [Fact]
    public void OlusturulanSatir_SimdikiZamanlaDamgalanir()
    {
        var before = DateTime.UtcNow;

        var entry = AuditLogEntry.Create(Guid.NewGuid(), "x@atlas.local", "WikiPage.Deleted", null);

        var after = DateTime.UtcNow;
        Assert.InRange(entry.OccurredAtUtc, before, after);
    }

    [Fact]
    public void BosActionIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() => AuditLogEntry.Create(Guid.NewGuid(), "x@atlas.local", "   ", null));
    }

    [Fact]
    public void ResourceIdOlmadanDa_SatirOlusabilir()
    {
        var entry = AuditLogEntry.Create(Guid.NewGuid(), "x@atlas.local", "WikiPage.Created", null);

        Assert.Null(entry.ResourceId);
    }
}
