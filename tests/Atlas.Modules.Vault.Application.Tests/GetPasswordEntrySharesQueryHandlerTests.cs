using Atlas.Modules.Vault.Application.PasswordEntries.Queries;
using Atlas.Modules.Vault.Application.Tests.Fakes;
using Atlas.Modules.Vault.Domain.Entities;
using Atlas.Shared.Testing;

namespace Atlas.Modules.Vault.Application.Tests;

public class GetPasswordEntrySharesQueryHandlerTests
{
    private static PasswordEntry CreateEntry(Guid ownerId) =>
        PasswordEntry.Create("Sunucu Şifresi", "admin", "enc:secret", null, null, null, null, ownerId, "owner@atlas.local");

    private static GetPasswordEntrySharesQueryHandler CreateHandler(
        FakePasswordEntryRepository repository, FakePasswordEntryShareRepository shareRepository,
        FakeCurrentUserAccessor currentUser)
        => new(repository, shareRepository, currentUser);

    [Fact]
    public async Task Handle_KayitBulunamazsa_NullDoner()
    {
        var handler = CreateHandler(
            new FakePasswordEntryRepository(), new FakePasswordEntryShareRepository(),
            new FakeCurrentUserAccessor(department: null));

        var result = await handler.Handle(new GetPasswordEntrySharesQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_SahibiOlmayanVeAdminOlmayanKullanici_NullDoner()
    {
        // Kaydın PAYLAŞILDIĞI kullanıcı bile "kiminle paylaşıldı" listesini
        // göremiyor - GetPasswordEntrySharesQueryHandler'daki BİLİNÇLİ kural
        // (sadece sahibi/Admin görebilir).
        var ownerId = Guid.NewGuid();
        var sharedWithId = Guid.NewGuid();
        var entry = CreateEntry(ownerId);
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var shareRepository = new FakePasswordEntryShareRepository();
        shareRepository.Shares.Add(PasswordEntryShare.Create(entry.Id, sharedWithId, "hedef@atlas.local", ownerId));

        var handler = CreateHandler(
            repository, shareRepository, new FakeCurrentUserAccessor(department: null, userId: sharedWithId));

        var result = await handler.Handle(new GetPasswordEntrySharesQuery(entry.Id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_Sahibi_PaylasimListesiniGorur()
    {
        var ownerId = Guid.NewGuid();
        var sharedWithId = Guid.NewGuid();
        var entry = CreateEntry(ownerId);
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var shareRepository = new FakePasswordEntryShareRepository();
        shareRepository.Shares.Add(PasswordEntryShare.Create(entry.Id, sharedWithId, "hedef@atlas.local", ownerId));

        var handler = CreateHandler(
            repository, shareRepository, new FakeCurrentUserAccessor(department: null, userId: ownerId));

        var result = await handler.Handle(new GetPasswordEntrySharesQuery(entry.Id), CancellationToken.None);

        Assert.NotNull(result);
        var share = Assert.Single(result!);
        Assert.Equal(sharedWithId, share.SharedWithUserId);
        Assert.Equal("hedef@atlas.local", share.SharedWithEmail);
    }

    [Fact]
    public async Task Handle_Admin_BaskasininPaylasimListesiniGorur()
    {
        var ownerId = Guid.NewGuid();
        var sharedWithId = Guid.NewGuid();
        var entry = CreateEntry(ownerId);
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var shareRepository = new FakePasswordEntryShareRepository();
        shareRepository.Shares.Add(PasswordEntryShare.Create(entry.Id, sharedWithId, "hedef@atlas.local", ownerId));

        var handler = CreateHandler(
            repository, shareRepository,
            new FakeCurrentUserAccessor(department: null, isAdmin: true, userId: Guid.NewGuid()));

        var result = await handler.Handle(new GetPasswordEntrySharesQuery(entry.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result!);
    }
}
