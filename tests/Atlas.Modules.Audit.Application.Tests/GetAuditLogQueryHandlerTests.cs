using Atlas.Modules.Audit.Application.AuditLog.Queries;
using Atlas.Modules.Audit.Application.Tests.Fakes;

namespace Atlas.Modules.Audit.Application.Tests;

public class GetAuditLogQueryHandlerTests
{
    [Fact]
    public async Task Handle_FiltreleriOldugoGibiRepositoryeGecirir()
    {
        var repository = new FakeAuditLogRepository();
        var handler = new GetAuditLogQueryHandler(repository);
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        await handler.Handle(new GetAuditLogQuery("WikiPage.Created", from, to, 2, 30), CancellationToken.None);

        Assert.Equal("WikiPage.Created", repository.LastAction);
        Assert.Equal(from, repository.LastFromUtc);
        Assert.Equal(to, repository.LastToUtc);
        Assert.Equal(2, repository.LastPageNumber);
        Assert.Equal(30, repository.LastPageSize);
    }

    [Fact]
    public async Task Handle_CokBuyukPageSizei_100eSabitler()
    {
        var repository = new FakeAuditLogRepository();
        var handler = new GetAuditLogQueryHandler(repository);

        await handler.Handle(new GetAuditLogQuery(PageSize: 500), CancellationToken.None);

        Assert.Equal(100, repository.LastPageSize);
    }

    [Fact]
    public async Task Handle_SifirYaDaNegatifPageNumberi_1eSabitler()
    {
        var repository = new FakeAuditLogRepository();
        var handler = new GetAuditLogQueryHandler(repository);

        await handler.Handle(new GetAuditLogQuery(PageNumber: -5), CancellationToken.None);

        Assert.Equal(1, repository.LastPageNumber);
    }
}
