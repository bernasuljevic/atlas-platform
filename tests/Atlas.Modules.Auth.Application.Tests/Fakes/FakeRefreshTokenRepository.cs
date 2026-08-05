using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Domain.Entities;

namespace Atlas.Modules.Auth.Application.Tests.Fakes;

public class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = new();
    public int SaveChangesCallCount { get; private set; }

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => Task.FromResult(Tokens.FirstOrDefault(t => t.Token == token));

    public Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        Tokens.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
