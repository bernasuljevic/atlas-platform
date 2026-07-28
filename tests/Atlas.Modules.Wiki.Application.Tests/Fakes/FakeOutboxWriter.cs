using Atlas.Modules.Wiki.Application.Abstractions;
using MediatR;

namespace Atlas.Modules.Wiki.Application.Tests.Fakes;

public class FakeOutboxWriter : IOutboxWriter
{
    public List<INotification> Enqueued { get; } = new();

    public void Enqueue(INotification notification)
    {
        Enqueued.Add(notification);
    }
}
