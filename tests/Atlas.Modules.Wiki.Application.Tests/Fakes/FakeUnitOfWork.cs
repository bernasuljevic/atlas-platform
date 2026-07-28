using Atlas.Modules.Wiki.Application.Abstractions;

namespace Atlas.Modules.Wiki.Application.Tests.Fakes;

public class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
