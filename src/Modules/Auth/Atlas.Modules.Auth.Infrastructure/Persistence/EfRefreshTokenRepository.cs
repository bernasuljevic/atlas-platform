using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Auth.Infrastructure.Persistence;

public class EfRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _context;

    public EfRefreshTokenRepository(AuthDbContext context)
    {
        _context = context;
    }

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token, ct);

    public Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        _context.RefreshTokens.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
