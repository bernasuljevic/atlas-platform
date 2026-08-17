using Atlas.Modules.Vault.Application.Abstractions;
using Atlas.Modules.Vault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Vault.Infrastructure.Persistence;

public class EfPasswordEntryShareRepository : IPasswordEntryShareRepository
{
    private readonly VaultDbContext _context;

    public EfPasswordEntryShareRepository(VaultDbContext context)
    {
        _context = context;
    }

    public Task<bool> IsSharedWithAsync(Guid passwordEntryId, Guid userId, CancellationToken cancellationToken) =>
        _context.PasswordEntryShares.AnyAsync(
            s => s.PasswordEntryId == passwordEntryId && s.SharedWithUserId == userId, cancellationToken);

    public Task<PasswordEntryShare?> GetAsync(
        Guid passwordEntryId, Guid sharedWithUserId, CancellationToken cancellationToken) =>
        _context.PasswordEntryShares.FirstOrDefaultAsync(
            s => s.PasswordEntryId == passwordEntryId && s.SharedWithUserId == sharedWithUserId, cancellationToken);

    public async Task<IReadOnlyList<PasswordEntryShare>> GetSharesForEntryAsync(
        Guid passwordEntryId, CancellationToken cancellationToken) =>
        await _context.PasswordEntryShares
            .Where(s => s.PasswordEntryId == passwordEntryId)
            .OrderByDescending(s => s.SharedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetEntryIdsSharedWithUserAsync(
        Guid userId, CancellationToken cancellationToken) =>
        await _context.PasswordEntryShares
            .Where(s => s.SharedWithUserId == userId)
            .Select(s => s.PasswordEntryId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PasswordEntryShare share, CancellationToken cancellationToken)
    {
        _context.PasswordEntryShares.Add(share);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(PasswordEntryShare share, CancellationToken cancellationToken)
    {
        _context.PasswordEntryShares.Remove(share);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ExecuteDeleteAsync BİLEREK KULLANILMIYOR - Ders #22'deki AYNI gerekçe
    // (InMemory sağlayıcısında ÇALIŞMIYOR); Vault'un test host'unda şu an
    // gerçek SQL Server kullanılıyor olsa da (bkz. AtlasApiFactory.cs'te
    // "VaultDbContext" için InMemory dönüşümü YOK), bu satır sayısı zaten
    // küçük bir işlem için ekstra bir sağlayıcı-bağımlılığı riski almanın
    // gereği yok.
    public async Task DeleteAllForEntryAsync(Guid passwordEntryId, CancellationToken cancellationToken)
    {
        var shares = await _context.PasswordEntryShares
            .Where(s => s.PasswordEntryId == passwordEntryId)
            .ToListAsync(cancellationToken);

        if (shares.Count == 0) return;

        _context.PasswordEntryShares.RemoveRange(shares);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
