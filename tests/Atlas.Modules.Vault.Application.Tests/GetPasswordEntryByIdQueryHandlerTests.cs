using Atlas.Modules.Vault.Application.PasswordEntries.Queries;
using Atlas.Modules.Vault.Application.Tests.Fakes;
using Atlas.Modules.Vault.Domain.Entities;
using Atlas.Shared.Testing;

namespace Atlas.Modules.Vault.Application.Tests;

public class GetPasswordEntryByIdQueryHandlerTests
{
    private static PasswordEntry CreateEntry(Guid ownerId) =>
        PasswordEntry.Create("Sunucu Şifresi", "admin", "enc:secret", null, null, null, null, ownerId, "owner@atlas.local");

    private static GetPasswordEntryByIdQueryHandler CreateHandler(
        FakePasswordEntryRepository repository, FakePasswordEntryShareRepository shareRepository,
        FakeCurrentUserAccessor currentUser)
        => new(repository, shareRepository, currentUser);

    [Fact]
    public async Task Handle_KayitBulunamazsa_NullDoner()
    {
        var handler = CreateHandler(
            new FakePasswordEntryRepository(), new FakePasswordEntryShareRepository(),
            new FakeCurrentUserAccessor(department: null));

        var result = await handler.Handle(new GetPasswordEntryByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_Sahibi_KaydiGorur()
    {
        var ownerId = Guid.NewGuid();
        var entry = CreateEntry(ownerId);
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var handler = CreateHandler(
            repository, new FakePasswordEntryShareRepository(),
            new FakeCurrentUserAccessor(department: null, userId: ownerId));

        var result = await handler.Handle(new GetPasswordEntryByIdQuery(entry.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(entry.Id, result!.Id);
    }

    [Fact]
    public async Task Handle_Admin_BaskasininKaydiniGorur()
    {
        var entry = CreateEntry(Guid.NewGuid());
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var handler = CreateHandler(
            repository, new FakePasswordEntryShareRepository(),
            new FakeCurrentUserAccessor(department: null, isAdmin: true, userId: Guid.NewGuid()));

        var result = await handler.Handle(new GetPasswordEntryByIdQuery(entry.Id), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_PaylasilanKullanici_KaydiGorur()
    {
        // Vault paylaşım modeli (D grubu, Gün 1) - owner-or-Admin dışındaki
        // ÜÇÜNCÜ istisna: kayıt bu kullanıcıyla paylaşılmışsa da görebiliyor.
        var ownerId = Guid.NewGuid();
        var sharedWithId = Guid.NewGuid();
        var entry = CreateEntry(ownerId);
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var shareRepository = new FakePasswordEntryShareRepository();
        shareRepository.Shares.Add(PasswordEntryShare.Create(entry.Id, sharedWithId, "hedef@atlas.local", ownerId));

        var handler = CreateHandler(
            repository, shareRepository, new FakeCurrentUserAccessor(department: null, userId: sharedWithId));

        var result = await handler.Handle(new GetPasswordEntryByIdQuery(entry.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(entry.Id, result!.Id);
    }

    [Fact]
    public async Task Handle_IlgisizKullanici_NullDoner()
    {
        // Ne sahibi, ne Admin, ne de kendisiyle paylaşılmış - varlığı bile
        // sızdırmadan null dönmeli (404, 403 DEĞİL).
        var ownerId = Guid.NewGuid();
        var entry = CreateEntry(ownerId);
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var handler = CreateHandler(
            repository, new FakePasswordEntryShareRepository(),
            new FakeCurrentUserAccessor(department: null, userId: Guid.NewGuid()));

        var result = await handler.Handle(new GetPasswordEntryByIdQuery(entry.Id), CancellationToken.None);

        Assert.Null(result);
    }
}
