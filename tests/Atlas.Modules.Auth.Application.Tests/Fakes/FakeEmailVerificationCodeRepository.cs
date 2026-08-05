using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Domain.Entities;

namespace Atlas.Modules.Auth.Application.Tests.Fakes;

public class FakeEmailVerificationCodeRepository : IEmailVerificationCodeRepository
{
    public List<EmailVerificationCode> Codes { get; } = new();

    public Task<EmailVerificationCode?> GetLatestActiveForUserAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(Codes
            .Where(c => c.UserId == userId && c.UsedAtUtc is null)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefault());

    public Task AddAsync(EmailVerificationCode code, CancellationToken ct = default)
    {
        Codes.Add(code);
        return Task.CompletedTask;
    }

    public Task InvalidateActiveCodesForUserAsync(Guid userId, CancellationToken ct = default)
    {
        foreach (var code in Codes.Where(c => c.UserId == userId && c.UsedAtUtc is null))
        {
            code.MarkUsed();
        }
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
