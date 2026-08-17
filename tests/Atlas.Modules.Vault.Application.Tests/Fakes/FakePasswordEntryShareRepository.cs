using Atlas.Modules.Vault.Application.Abstractions;
using Atlas.Modules.Vault.Domain.Entities;

namespace Atlas.Modules.Vault.Application.Tests.Fakes;

public class FakePasswordEntryShareRepository : IPasswordEntryShareRepository
{
    public List<PasswordEntryShare> Shares { get; } = new();
    public List<PasswordEntryShare> Removed { get; } = new();

    // DeleteAllForEntryAsync'in HANGİ entryId'lerle çağrıldığını doğrulamak
    // için - DeletePasswordEntryCommandHandlerTests'in asıl kontrol ettiği
    // "silme sırasında paylaşımlar da temizleniyor mu" sorusu bu liste
    // üzerinden doğrulanıyor.
    public List<Guid> DeleteAllForEntryCalls { get; } = new();

    public Task<bool> IsSharedWithAsync(Guid passwordEntryId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Shares.Any(s => s.PasswordEntryId == passwordEntryId && s.SharedWithUserId == userId));

    public Task<PasswordEntryShare?> GetAsync(Guid passwordEntryId, Guid sharedWithUserId, CancellationToken cancellationToken) =>
        Task.FromResult(Shares.FirstOrDefault(
            s => s.PasswordEntryId == passwordEntryId && s.SharedWithUserId == sharedWithUserId));

    public Task<IReadOnlyList<PasswordEntryShare>> GetSharesForEntryAsync(Guid passwordEntryId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PasswordEntryShare>>(
            Shares.Where(s => s.PasswordEntryId == passwordEntryId).ToList());

    public Task<IReadOnlyList<Guid>> GetEntryIdsSharedWithUserAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>(
            Shares.Where(s => s.SharedWithUserId == userId).Select(s => s.PasswordEntryId).Distinct().ToList());

    public Task AddAsync(PasswordEntryShare share, CancellationToken cancellationToken)
    {
        Shares.Add(share);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(PasswordEntryShare share, CancellationToken cancellationToken)
    {
        Shares.RemoveAll(s => s.Id == share.Id);
        Removed.Add(share);
        return Task.CompletedTask;
    }

    public Task DeleteAllForEntryAsync(Guid passwordEntryId, CancellationToken cancellationToken)
    {
        DeleteAllForEntryCalls.Add(passwordEntryId);
        Shares.RemoveAll(s => s.PasswordEntryId == passwordEntryId);
        return Task.CompletedTask;
    }
}
