using Atlas.Modules.Vault.Application.PasswordEntries.Commands;
using Atlas.Modules.Vault.Application.Tests.Fakes;
using Atlas.Modules.Vault.Domain.Entities;
using Atlas.Shared.Testing;

namespace Atlas.Modules.Vault.Application.Tests;

public class RevealPasswordEntryCommandHandlerTests
{
    private static PasswordEntry CreateEntry(Guid ownerId, string title = "Sunucu Şifresi") =>
        PasswordEntry.Create(title, "admin", "enc:gizli-parola", null, null, null, null, ownerId, "owner@atlas.local");

    private static RevealPasswordEntryCommandHandler CreateHandler(
        FakePasswordEntryRepository repository, FakePasswordEntryShareRepository shareRepository,
        FakeCurrentUserAccessor currentUser)
        => new(repository, shareRepository, new FakePasswordEncryptor(), currentUser);

    [Fact]
    public async Task Handle_KayitBulunamazsa_HataFirlatir()
    {
        var handler = CreateHandler(
            new FakePasswordEntryRepository(), new FakePasswordEntryShareRepository(),
            new FakeCurrentUserAccessor(department: null));

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new RevealPasswordEntryCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Sahibi_ParolayiGorurVeMarkAccessedCagirilir()
    {
        var ownerId = Guid.NewGuid();
        var entry = CreateEntry(ownerId, title: "Prod Sunucu");
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var handler = CreateHandler(
            repository, new FakePasswordEntryShareRepository(),
            new FakeCurrentUserAccessor(department: null, userId: ownerId));

        var command = new RevealPasswordEntryCommand(entry.Id);
        var plainPassword = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("gizli-parola", plainPassword);
        Assert.Equal("Prod Sunucu", command.AuditDetails);
        Assert.NotNull(entry.LastAccessedAtUtc);
        Assert.Single(repository.Updated);
    }

    [Fact]
    public async Task Handle_Admin_BaskasininParolasiniGorebilir()
    {
        var entry = CreateEntry(Guid.NewGuid());
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var handler = CreateHandler(
            repository, new FakePasswordEntryShareRepository(),
            new FakeCurrentUserAccessor(department: null, isAdmin: true, userId: Guid.NewGuid()));

        var plainPassword = await handler.Handle(new RevealPasswordEntryCommand(entry.Id), CancellationToken.None);

        Assert.Equal("gizli-parola", plainPassword);
    }

    [Fact]
    public async Task Handle_PaylasilanKullanici_ParolayiGorebilir()
    {
        // Vault paylaşım modeli (D grubu, Gün 1) - paylaşımın ASIL AMACI: alıcı
        // sadece kaydı GÖRMÜYOR, parolayı da AÇABİLİYOR.
        var ownerId = Guid.NewGuid();
        var sharedWithId = Guid.NewGuid();
        var entry = CreateEntry(ownerId);
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var shareRepository = new FakePasswordEntryShareRepository();
        shareRepository.Shares.Add(PasswordEntryShare.Create(entry.Id, sharedWithId, "hedef@atlas.local", ownerId));

        var handler = CreateHandler(
            repository, shareRepository, new FakeCurrentUserAccessor(department: null, userId: sharedWithId));

        var plainPassword = await handler.Handle(new RevealPasswordEntryCommand(entry.Id), CancellationToken.None);

        Assert.Equal("gizli-parola", plainPassword);
    }

    [Fact]
    public async Task Handle_IlgisizKullanici_YetkisizHatasiFirlatir()
    {
        var ownerId = Guid.NewGuid();
        var entry = CreateEntry(ownerId);
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var handler = CreateHandler(
            repository, new FakePasswordEntryShareRepository(),
            new FakeCurrentUserAccessor(department: null, userId: Guid.NewGuid()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new RevealPasswordEntryCommand(entry.Id), CancellationToken.None));
    }
}
