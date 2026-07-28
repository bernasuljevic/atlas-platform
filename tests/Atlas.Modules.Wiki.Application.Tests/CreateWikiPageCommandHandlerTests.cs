using Atlas.Modules.Wiki.Application.Tests.Fakes;
using Atlas.Modules.Wiki.Application.WikiPages.Commands;
using Atlas.Shared.Contracts;

namespace Atlas.Modules.Wiki.Application.Tests;

public class CreateWikiPageCommandHandlerTests
{
    private static CreateWikiPageCommandHandler CreateHandler(
        out FakeWikiPageRepository repository,
        out FakeOutboxWriter outboxWriter,
        out FakeUnitOfWork unitOfWork,
        string? viewerDepartment,
        bool viewerIsAdmin = false)
    {
        repository = new FakeWikiPageRepository();
        outboxWriter = new FakeOutboxWriter();
        unitOfWork = new FakeUnitOfWork();
        return new CreateWikiPageCommandHandler(
            repository,
            new FakeCurrentUserAccessor(viewerDepartment, viewerIsAdmin),
            outboxWriter,
            unitOfWork);
    }

    [Fact]
    public async Task NormalKullanici_IstediginFarkliDepartmaniGondersede_SayfaKendiDepartmaninaKaydedilir()
    {
        // Bu, Ders #10'daki okuma tarafı açığının yazma tarafındaki karşılığını
        // doğruluyor: IK'daki bir kullanıcı "departmentName: IT" gönderse bile,
        // sayfa GERÇEK departmanına (IK) kaydedilmeli - istemciden gelen değere
        // güvenilmemeli.
        var handler = CreateHandler(out var repository, out _, out _, viewerDepartment: "IK");
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
        var handler = CreateHandler(out var repository, out _, out _, viewerDepartment: null, viewerIsAdmin: true);
        var command = new CreateWikiPageCommand("Başlık", "İçerik", "IT", "Public");

        await handler.Handle(command, CancellationToken.None);

        Assert.Single(repository.AddedPages);
        Assert.Equal("IT", repository.AddedPages[0].DepartmentName);
    }

    [Fact]
    public async Task DepartmansizNormalKullanici_SayfaOlusturamaz()
    {
        var handler = CreateHandler(out _, out _, out _, viewerDepartment: null);
        var command = new CreateWikiPageCommand("Başlık", "İçerik", "IT", "Public");

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task SayfaOlusturulunca_OutboxaWikiPageCreatedEventEklenirVeTekBirKezKaydedilir()
    {
        // Outbox Pattern Gün 2'nin asıl doğrulaması: event doğrudan yayınlanmıyor
        // (eski IPublisher.Publish deseni), bunun yerine Outbox'a ekleniyor ve
        // WikiPage'in KENDİSİYLE birlikte TEK bir SaveChanges'te (atomik) yazılıyor.
        var handler = CreateHandler(
            out _, out var outboxWriter, out var unitOfWork, viewerDepartment: "IT");
        var command = new CreateWikiPageCommand("Başlık", "İçerik", "IT", "Public");

        await handler.Handle(command, CancellationToken.None);

        var enqueued = Assert.Single(outboxWriter.Enqueued);
        Assert.IsType<WikiPageCreatedEvent>(enqueued);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
