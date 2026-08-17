using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence;

public class EfWikiPageVersionRepository : IWikiPageVersionRepository
{
    private readonly WikiDbContext _context;

    public EfWikiPageVersionRepository(WikiDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(WikiPageVersion version, CancellationToken ct = default)
    {
        _context.WikiPageVersions.Add(version);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<WikiPageVersion>> GetByWikiPageIdAsync(Guid wikiPageId, CancellationToken ct = default)
        => await _context.WikiPageVersions
            .Where(v => v.WikiPageId == wikiPageId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);

    public async Task<WikiPageVersion?> GetByWikiPageIdAndVersionNumberAsync(
        Guid wikiPageId, int versionNumber, CancellationToken ct = default)
        => await _context.WikiPageVersions
            .FirstOrDefaultAsync(v => v.WikiPageId == wikiPageId && v.VersionNumber == versionNumber, ct);

    // ExecuteDeleteAsync BİLEREK KULLANILMIYOR - integration test host'unun
    // WikiDbContext'i EF Core InMemory sağlayıcısını kullanıyor (bkz.
    // AtlasApiFactory.cs), ve InMemory sağlayıcısı ExecuteDelete/ExecuteUpdate'i
    // DESTEKLEMİYOR (canlı CI'da 500 ile yakalandı - "InvalidOperationException:
    // ... provider does not support ExecuteDelete"). RemoveRange + AddAsync'teki
    // AYNI "stage et, SaveChanges'i ÇAĞIRMA" deseni hem InMemory/SQL Server
    // ikisinde de çalışıyor HEM de asıl amaçlanan atomikliği sağlıyor -
    // ExecuteDeleteAsync ZATEN HEMEN/AYRI çalışıyordu, DeleteWikiPageCommandHandler'ın
    // sonundaki tek SaveChangesAsync'in DIŞINDA kalıyordu.
    public async Task DeleteAllForWikiPageAsync(Guid wikiPageId, CancellationToken ct = default)
    {
        var versions = await _context.WikiPageVersions
            .Where(v => v.WikiPageId == wikiPageId)
            .ToListAsync(ct);
        _context.WikiPageVersions.RemoveRange(versions);
    }
}
