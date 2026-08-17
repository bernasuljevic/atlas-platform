using Atlas.Modules.Vault.Application.PasswordEntries.Commands;
using Atlas.Modules.Vault.Application.Tests.Fakes;
using Atlas.Modules.Vault.Domain.Entities;
using Atlas.Shared.Testing;

namespace Atlas.Modules.Vault.Application.Tests;

public class RemovePasswordEntryShareCommandHandlerTests
{
    private static PasswordEntry CreateEntry(Guid ownerId, string title = "Sunucu Şifresi") =>
        PasswordEntry.Create(title, "admin", "enc:secret", null, null, null, null, ownerId, "owner@atlas.local");

    private static RemovePasswordEntryShareCommandHandler CreateHandler(
        FakePasswordEntryRepository repository, FakePasswordEntryShareRepository shareRepository,
        FakeCurrentUserAccessor currentUser)
        => new(repository, shareRepository, currentUser);

    [Fact]
    public async Task Handle_KayitBulunamazsa_HataFirlatir()
    {
        var handler = CreateHandler(
            new FakePasswordEntryRepository(), new FakePasswordEntryShareRepository(),
            new FakeCurrentUserAccessor(department: null));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new RemovePasswordEntryShareCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SahibiOlmayanKullanici_YetkisizHatasiFirlatir()
    {
        var entry = CreateEntry(Guid.NewGuid());
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var handler = CreateHandler(
            repository, new FakePasswordEntryShareRepository(),
            new FakeCurrentUserAccessor(department: null, userId: Guid.NewGuid()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(
            new RemovePasswordEntryShareCommand(entry.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PaylasimBulunamazsa_HataFirlatir()
    {
        var ownerId = Guid.NewGuid();
        var entry = CreateEntry(ownerId);
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var handler = CreateHandler(
            repository, new FakePasswordEntryShareRepository(),
            new FakeCurrentUserAccessor(department: null, userId: ownerId));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new RemovePasswordEntryShareCommand(entry.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_BasariliKaldirma_ShareKaldirilirVeAuditDetailsDolar()
    {
        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var entry = CreateEntry(ownerId, title: "Prod Sunucu");
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var shareRepository = new FakePasswordEntryShareRepository();
        shareRepository.Shares.Add(PasswordEntryShare.Create(entry.Id, targetId, "hedef@atlas.local", ownerId));

        var handler = CreateHandler(
            repository, shareRepository, new FakeCurrentUserAccessor(department: null, userId: ownerId));

        var command = new RemovePasswordEntryShareCommand(entry.Id, targetId);
        await handler.Handle(command, CancellationToken.None);

        Assert.Empty(shareRepository.Shares);
        Assert.Single(shareRepository.Removed);
        Assert.Equal("Prod Sunucu -> hedef@atlas.local", command.AuditDetails);
    }

    [Fact]
    public async Task Handle_AdminSahibiOlmayanPaylasimiKaldirabilir()
    {
        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var entry = CreateEntry(ownerId);
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var shareRepository = new FakePasswordEntryShareRepository();
        shareRepository.Shares.Add(PasswordEntryShare.Create(entry.Id, targetId, "hedef@atlas.local", ownerId));

        var handler = CreateHandler(
            repository, shareRepository,
            new FakeCurrentUserAccessor(department: null, isAdmin: true, userId: Guid.NewGuid()));

        await handler.Handle(new RemovePasswordEntryShareCommand(entry.Id, targetId), CancellationToken.None);

        Assert.Empty(shareRepository.Shares);
    }
}
