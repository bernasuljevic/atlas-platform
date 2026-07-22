using Atlas.Modules.Auth.Domain.Entities;

namespace Atlas.Modules.Auth.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default);

    // Revoke() gibi mevcut bir entity üzerindeki değişiklikler EF Core tarafından
    // zaten izleniyor (tracked) - repository'nin ayrı bir "Update" metoduna
    // ihtiyacı yok, sadece en sonda tek bir SaveChangesAsync yeterli.
    Task SaveChangesAsync(CancellationToken ct = default);
}
