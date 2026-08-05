using Atlas.Modules.Wiki.Application.Tests.Fakes;
using Atlas.Shared.Testing;
using Atlas.Modules.Wiki.Application.WikiFolders.Commands;
using Atlas.Modules.Wiki.Domain.Entities;

namespace Atlas.Modules.Wiki.Application.Tests;

public class CreateWikiFolderCommandHandlerTests
{
    private static CreateWikiFolderCommandHandler CreateHandler(
        out FakeWikiFolderRepository repository,
        string? viewerDepartment,
        bool viewerIsAdmin = false)
    {
        repository = new FakeWikiFolderRepository();
        return new CreateWikiFolderCommandHandler(
            repository,
            new FakeCurrentUserAccessor(viewerDepartment, viewerIsAdmin),
            new FakeUnitOfWork());
    }

    [Fact]
    public async Task NormalKullanici_IstediginFarkliDepartmaniGondersede_KlasorKendiDepartmaninaKaydedilir()
    {
        var handler = CreateHandler(out var repository, viewerDepartment: "IK");
        var command = new CreateWikiFolderCommand("Bordro", "IT", null);

        await handler.Handle(command, CancellationToken.None);

        Assert.Single(repository.AddedFolders);
        Assert.Equal("IK", repository.AddedFolders[0].DepartmentName);
    }

    [Fact]
    public async Task Admin_IstedigiDepartmandaKlasorAcabilir()
    {
        var handler = CreateHandler(out var repository, viewerDepartment: null, viewerIsAdmin: true);
        var command = new CreateWikiFolderCommand("Bordro", "IK", null);

        await handler.Handle(command, CancellationToken.None);

        Assert.Single(repository.AddedFolders);
        Assert.Equal("IK", repository.AddedFolders[0].DepartmentName);
    }

    [Fact]
    public async Task DepartmansizNormalKullanici_KlasorOlusturamaz()
    {
        var handler = CreateHandler(out _, viewerDepartment: null);
        var command = new CreateWikiFolderCommand("Bordro", "IT", null);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task KendiDepartmaninKlasorununAltinaAltKlasorAcilabilir()
    {
        var repository = new FakeWikiFolderRepository();
        var parent = WikiFolder.Create("React", "IT", null, Guid.NewGuid());
        repository.AddedFolders.Add(parent);

        var handler = new CreateWikiFolderCommandHandler(
            repository, new FakeCurrentUserAccessor("IT"), new FakeUnitOfWork());
        var command = new CreateWikiFolderCommand("UI", "IT", parent.Id);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(2, repository.AddedFolders.Count);
        Assert.Equal(parent.Id, repository.AddedFolders[1].ParentFolderId);
    }

    [Fact]
    public async Task BaskaDepartmaninKlasorununAltina_AltKlasorAcilamaz()
    {
        var repository = new FakeWikiFolderRepository();
        var parent = WikiFolder.Create("İK Klasörü", "IK", null, Guid.NewGuid());
        repository.AddedFolders.Add(parent);

        var handler = new CreateWikiFolderCommandHandler(
            repository, new FakeCurrentUserAccessor("IT"), new FakeUnitOfWork());
        var command = new CreateWikiFolderCommand("Sızıntı", "IT", parent.Id);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task OlmayanUstKlasor_ArgumentExceptionAlir()
    {
        var handler = CreateHandler(out _, viewerDepartment: "IT");
        var command = new CreateWikiFolderCommand("UI", "IT", Guid.NewGuid());

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }
}
