using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Auth.Infrastructure.Persistence;

public class EfEmailVerificationCodeRepository : IEmailVerificationCodeRepository
{
    private readonly AuthDbContext _context;

    public EfEmailVerificationCodeRepository(AuthDbContext context)
    {
        _context = context;
    }

    public Task<EmailVerificationCode?> GetLatestActiveForUserAsync(Guid userId, CancellationToken ct = default)
        => _context.EmailVerificationCodes
            .Where(c => c.UserId == userId && c.UsedAtUtc == null)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

    public Task AddAsync(EmailVerificationCode code, CancellationToken ct = default)
    {
        _context.EmailVerificationCodes.Add(code);
        return Task.CompletedTask;
    }

    public async Task InvalidateActiveCodesForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var activeCodes = await _context.EmailVerificationCodes
            .Where(c => c.UserId == userId && c.UsedAtUtc == null)
            .ToListAsync(ct);

        foreach (var code in activeCodes)
        {
            code.MarkUsed();
        }
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
