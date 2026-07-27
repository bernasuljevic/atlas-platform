using Atlas.Modules.Wiki.Application.Tests.Fakes;
using Atlas.Modules.Wiki.Application.WikiPages.Commands;

namespace Atlas.Modules.Wiki.Application.Tests;

public class CreateWikiPageCommandHandlerTests
{
    private static CreateWikiPageCommandHandler CreateHandler(
        out FakeWikiPageRepository repository, string? viewerDepartment, bool viewerIsAdmin = false)
    {
        repository = new FakeWikiPageRepository();
        return new CreateWikiPageCommandHandler(
            repository,
            new FakeCurrentUserAccessor(viewerDepartment, viewerIsAdmin),
            new FakePublisher());
    }

    [Fact]
    public async Task NormalKullanici_IstediginFarkliDepartmaniGondersede_SayfaKendiDepartmaninaKaydedilir()
    {
        // Bu, Ders #10'daki okuma tarafı açığının yazma tarafındaki karşılığını
        // doğruluyor: IK'daki bir kullanıcı "departmentName: IT" gönderse bile,
        // sayfa GERÇEK departmanına (IK) kaydedilmeli - istemciden gelen değere
        // güvenilmemeli.
        var handler = CreateHandler(out var repository, viewerDepartment: "IK");
        var command = new CreateWikiPageCommand("Başlık", "İçerik", "IT", "Public");

        await handler.Handle(command, CancellationToken.None);

        Assert.Single(repository.AddedPages);
        Assert.Equal("IK", repository.AddedPages[0].DepartmentName);
    }

    [Fact]
    public async Task Admin_IstedigiDepartmaniSecebilir()
    {
        // Okuma tarafındaki bypass'la simetrik - Admin, kendi departmanı olmasa
        // (ya da farklı olsa) bile istediği departmana sayfa ekleyebiliyor.
        var handler = CreateHandler(out var repository, viewerDepartment: null, viewerIsAdmin: true);
        var command = new CreateWikiPageCommand("Başlık", "İçerik", "IT", "Public");

        await handler.Handle(command, CancellationToken.None);

        Assert.Single(repository.AddedPages);
        Assert.Equal("IT", repository.AddedPages[0].DepartmentName);
    }

    [Fact]
    public async Task DepartmansizNormalKullanici_SayfaOlusturamaz()
    {
        var handler = CreateHandler(out _, viewerDepartment: null);
        var command = new CreateWikiPageCommand("Başlık", "İçerik", "IT", "Public");

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }
}
