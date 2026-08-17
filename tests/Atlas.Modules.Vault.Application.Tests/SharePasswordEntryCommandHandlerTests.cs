using Atlas.Modules.Vault.Application.PasswordEntries.Commands;
using Atlas.Modules.Vault.Application.Tests.Fakes;
using Atlas.Modules.Vault.Domain.Entities;
using Atlas.Shared.Contracts;
using Atlas.Shared.Testing;

namespace Atlas.Modules.Vault.Application.Tests;

public class SharePasswordEntryCommandHandlerTests
{
    private static PasswordEntry CreateEntry(Guid ownerId, string title = "Sunucu Şifresi") =>
        PasswordEntry.Create(title, "admin", "enc:secret", null, null, null, null, ownerId, "owner@atlas.local");

    private static SharePasswordEntryCommandHandler CreateHandler(
        FakePasswordEntryRepository repository, FakePasswordEntryShareRepository shareRepository,
        FakeUserLookupService userLookup, FakeCurrentUserAccessor currentUser)
        => new(repository, shareRepository, userLookup, currentUser);

    [Fact]
    public async Task Handle_KayitBulunamazsa_HataFirlatir()
    {
        var handler = CreateHandler(
            new FakePasswordEntryRepository(), new FakePasswordEntryShareRepository(),
            new FakeUserLookupService(), new FakeCurrentUserAccessor(department: null));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new SharePasswordEntryCommand(Guid.NewGuid(), "hedef@atlas.local"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SahibiOlmayanKullanici_YetkisizHatasiFirlatir()
    {
        var entry = CreateEntry(Guid.NewGuid());
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var handler = CreateHandler(
            repository, new FakePasswordEntryShareRepository(), new FakeUserLookupService(),
            new FakeCurrentUserAccessor(department: null, userId: Guid.NewGuid()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(
            new SharePasswordEntryCommand(entry.Id, "hedef@atlas.local"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_HedefKullaniciBulunamazsa_HataFirlatir()
    {
        var ownerId = Guid.NewGuid();
        var entry = CreateEntry(ownerId);
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        // FakeUserLookupService BOŞ - "yok" olan bir e-posta.
        var handler = CreateHandler(
            repository, new FakePasswordEntryShareRepository(), new FakeUserLookupService(),
            new FakeCurrentUserAccessor(department: null, userId: ownerId));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new SharePasswordEntryCommand(entry.Id, "olmayan@atlas.local"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_KendiKendinePaylasimDenemesi_HataFirlatir()
    {
        var ownerId = Guid.NewGuid();
        var entry = CreateEntry(ownerId);
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var userLookup = new FakeUserLookupService();
        // Sahibinin e-postası kendisiyle aynı - sahip kendi kaydını "kendisiyle" paylaşmaya çalışıyor.
        userLookup.Users.Add(new UserSummary(ownerId, "owner@atlas.local", "Sahip"));

        var handler = CreateHandler(
            repository, new FakePasswordEntryShareRepository(), userLookup,
            new FakeCurrentUserAccessor(department: null, userId: ownerId));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new SharePasswordEntryCommand(entry.Id, "owner@atlas.local"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ZatenPaylasilmisKayit_HataFirlatir()
    {
        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var entry = CreateEntry(ownerId);
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var shareRepository = new FakePasswordEntryShareRepository();
        shareRepository.Shares.Add(PasswordEntryShare.Create(entry.Id, targetId, "hedef@atlas.local", ownerId));

        var userLookup = new FakeUserLookupService();
        userLookup.Users.Add(new UserSummary(targetId, "hedef@atlas.local", "Hedef"));

        var handler = CreateHandler(
            repository, shareRepository, userLookup, new FakeCurrentUserAccessor(department: null, userId: ownerId));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new SharePasswordEntryCommand(entry.Id, "hedef@atlas.local"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_BasariliPaylasim_ShareEklenirVeAuditDetailsDolar()
    {
        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var entry = CreateEntry(ownerId, title: "Prod Sunucu");
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var shareRepository = new FakePasswordEntryShareRepository();
        var userLookup = new FakeUserLookupService();
        userLookup.Users.Add(new UserSummary(targetId, "hedef@atlas.local", "Hedef"));

        var handler = CreateHandler(
            repository, shareRepository, userLookup, new FakeCurrentUserAccessor(department: null, userId: ownerId));

        var command = new SharePasswordEntryCommand(entry.Id, "hedef@atlas.local");
        await handler.Handle(command, CancellationToken.None);

        var share = Assert.Single(shareRepository.Shares);
        Assert.Equal(entry.Id, share.PasswordEntryId);
        Assert.Equal(targetId, share.SharedWithUserId);
        Assert.Equal("hedef@atlas.local", share.SharedWithEmail);
        Assert.Equal(ownerId, share.SharedByUserId);
        Assert.Equal("Prod Sunucu -> hedef@atlas.local", command.AuditDetails);
    }

    [Fact]
    public async Task Handle_AdminSahibiOlmayanKaydiPaylasabilir()
    {
        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var entry = CreateEntry(ownerId);
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var shareRepository = new FakePasswordEntryShareRepository();
        var userLookup = new FakeUserLookupService();
        userLookup.Users.Add(new UserSummary(targetId, "hedef@atlas.local", "Hedef"));

        var adminId = Guid.NewGuid();
        var handler = CreateHandler(
            repository, shareRepository, userLookup,
            new FakeCurrentUserAccessor(department: null, isAdmin: true, userId: adminId));

        await handler.Handle(new SharePasswordEntryCommand(entry.Id, "hedef@atlas.local"), CancellationToken.None);

        Assert.Single(shareRepository.Shares);
    }
}
