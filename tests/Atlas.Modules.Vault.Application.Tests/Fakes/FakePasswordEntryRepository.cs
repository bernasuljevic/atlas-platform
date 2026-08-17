using Atlas.Modules.Vault.Application.Abstractions;
using Atlas.Modules.Vault.Domain.Entities;

namespace Atlas.Modules.Vault.Application.Tests.Fakes;

// FakeDocumentRepository'nin (Documents.Application.Tests) AYNI deseni -
// gerçek bir DbContext yok, basit bir bellek-içi liste. GetAllAsync'in
// gerçek implementasyonundaki "owner OR shared" birleştirme sorgusu
// EfPasswordEntryRepository'de (Infrastructure) yaşıyor - bu Fake sadece
// Handler'ın hangi metotları hangi argümanlarla çağırdığını doğrulamaya
// yetecek kadar gerçekçi, viewerUserId'ye göre filtreleme YAPMIYOR (bu
// dosyanın kapsamındaki testler GetAllAsync'i hiç kullanmıyor).
public class FakePasswordEntryRepository : IPasswordEntryRepository
{
    public List<PasswordEntry> Entries { get; } = new();
    public List<PasswordEntry> Deleted { get; } = new();
    public List<PasswordEntry> Updated { get; } = new();

    public Task<PasswordEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Entries.FirstOrDefault(e => e.Id == id));

    public Task<IReadOnlyList<PasswordEntry>> GetAllAsync(Guid? viewerUserId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PasswordEntry>>(Entries.ToList());

    public Task AddAsync(PasswordEntry entry, CancellationToken cancellationToken)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PasswordEntry entry, CancellationToken cancellationToken)
    {
        Updated.Add(entry);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PasswordEntry entry, CancellationToken cancellationToken)
    {
        Entries.RemoveAll(e => e.Id == entry.Id);
        Deleted.Add(entry);
        return Task.CompletedTask;
    }
}
