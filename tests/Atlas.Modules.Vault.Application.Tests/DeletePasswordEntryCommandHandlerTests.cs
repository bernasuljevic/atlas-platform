using Atlas.Modules.Vault.Application.PasswordEntries.Commands;
using Atlas.Modules.Vault.Application.Tests.Fakes;
using Atlas.Modules.Vault.Domain.Entities;
using Atlas.Shared.Testing;

namespace Atlas.Modules.Vault.Application.Tests;

public class DeletePasswordEntryCommandHandlerTests
{
    private static PasswordEntry CreateEntry(Guid ownerId, string title = "Sunucu Şifresi") =>
        PasswordEntry.Create(title, "admin", "enc:secret", null, null, null, null, ownerId, "owner@atlas.local");

    private static DeletePasswordEntryCommandHandler CreateHandler(
        FakePasswordEntryRepository repository, FakePasswordEntryShareRepository shareRepository,
        FakeCurrentUserAccessor currentUser)
        => new(repository, shareRepository, currentUser);

    [Fact]
    public async Task Handle_KayitBulunamazsa_HataFirlatir()
    {
        var handler = CreateHandler(
            new FakePasswordEntryRepository(), new FakePasswordEntryShareRepository(),
            new FakeCurrentUserAccessor(department: null));

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new DeletePasswordEntryCommand(Guid.NewGuid()), CancellationToken.None));
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

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new DeletePasswordEntryCommand(entry.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_BasariliSilme_PaylasimlarDaTemizlenir()
    {
        // Vault paylaşım modeli (D grubu, Gün 1) - DeleteWikiPageCommandHandler'ın
        // versiyon geçmişini temizlemesiyle AYNI gerekçe: "yetim" paylaşım
        // satırları kalmamalı.
        var ownerId = Guid.NewGuid();
        var entry = CreateEntry(ownerId, title: "Prod Sunucu");
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var shareRepository = new FakePasswordEntryShareRepository();
        shareRepository.Shares.Add(PasswordEntryShare.Create(entry.Id, Guid.NewGuid(), "a@atlas.local", ownerId));
        shareRepository.Shares.Add(PasswordEntryShare.Create(entry.Id, Guid.NewGuid(), "b@atlas.local", ownerId));

        var handler = CreateHandler(
            repository, shareRepository, new FakeCurrentUserAccessor(department: null, userId: ownerId));

        var command = new DeletePasswordEntryCommand(entry.Id);
        await handler.Handle(command, CancellationToken.None);

        Assert.Single(shareRepository.DeleteAllForEntryCalls);
        Assert.Equal(entry.Id, shareRepository.DeleteAllForEntryCalls[0]);
        Assert.Empty(shareRepository.Shares);
        Assert.Empty(repository.Entries);
        Assert.Equal("Prod Sunucu", command.AuditDetails);
    }

    [Fact]
    public async Task Handle_AdminBaskasininKaydiniSilebilir()
    {
        var entry = CreateEntry(Guid.NewGuid());
        var repository = new FakePasswordEntryRepository();
        repository.Entries.Add(entry);

        var handler = CreateHandler(
            repository, new FakePasswordEntryShareRepository(),
            new FakeCurrentUserAccessor(department: null, isAdmin: true, userId: Guid.NewGuid()));

        await handler.Handle(new DeletePasswordEntryCommand(entry.Id), CancellationToken.None);

        Assert.Empty(repository.Entries);
    }
}
