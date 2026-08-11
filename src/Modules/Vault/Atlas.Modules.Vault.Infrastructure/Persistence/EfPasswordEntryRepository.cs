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

    public async Task<IReadOnlyList<PasswordEntry>> GetAllAsync(Guid? ownerUserId, CancellationToken cancellationToken)
    {
        var query = _context.PasswordEntries.AsQueryable();

        if (ownerUserId is not null)
            query = query.Where(p => p.CreatedByUserId == ownerUserId.Value);

        return await query.OrderByDescending(p => p.CreatedAtUtc).ToListAsync(cancellationToken);
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
