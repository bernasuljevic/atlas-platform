using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Auth.Infrastructure.Persistence;

// InMemoryUserRepository'nin yerini alan gerçek implementasyon.
// IUserRepository imzası hiç değişmedi - Application katmanı bu değişimi FARK ETMEZ.
public class EfUserRepository : IUserRepository
{
    private readonly AuthDbContext _context;

    public EfUserRepository(AuthDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
        => await _context.Users.ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
    }
}
