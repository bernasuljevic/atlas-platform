using Atlas.Modules.Vault.Application.Abstractions;
using Atlas.Modules.Vault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Vault.Infrastructure.Persistence;

public class EfPasswordEntryRepository : IPasswordEntryRepository
{
    private readonly VaultDbContext _context;

    public EfPasswordEntryRepository(VaultDbContext context)
    {
        _context = context;
    }

    public Task<PasswordEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.PasswordEntries.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PasswordEntry>> GetAllAsync(Guid? viewerUserId, CancellationToken cancellationToken)
    {
        if (viewerUserId is null)
        {
            // Admin - filtre yok, TÜM kayıtlar.
            return await _context.PasswordEntries
                .OrderByDescending(p => p.CreatedAtUtc)
                .ToListAsync(cancellationToken);
        }

        // Vault paylaşım modeli (D grubu, Gün 1) - iki ayrı SORGU (tek bir
        // JOIN yerine) BİLİNÇLİ OLARAK basit tutuldu: Vault'un kendi "kişisel
        // kasa, küçük ölçek" varsayımında (bkz. bu dosyanın en üstündeki not)
        // bunun performans maliyeti önemsiz, okunabilirlik kazancı daha değerli.
        var sharedEntryIds = await _context.PasswordEntryShares
            .Where(s => s.SharedWithUserId == viewerUserId.Value)
            .Select(s => s.PasswordEntryId)
            .ToListAsync(cancellationToken);

        return await _context.PasswordEntries
            .Where(p => p.CreatedByUserId == viewerUserId.Value || sharedEntryIds.Contains(p.Id))
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PasswordEntry entry, CancellationToken cancellationToken)
    {
        _context.PasswordEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PasswordEntry entry, CancellationToken cancellationToken)
    {
        _context.PasswordEntries.Update(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(PasswordEntry entry, CancellationToken cancellationToken)
    {
        _context.PasswordEntries.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
