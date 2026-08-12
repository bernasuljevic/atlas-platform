using Atlas.Modules.Documents.Application.Abstractions;

namespace Atlas.Modules.Documents.Application.Tests.Fakes;

public class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
