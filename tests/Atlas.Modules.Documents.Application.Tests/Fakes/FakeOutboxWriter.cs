using Atlas.Modules.Documents.Application.Abstractions;
using MediatR;

namespace Atlas.Modules.Documents.Application.Tests.Fakes;

public class FakeOutboxWriter : IOutboxWriter
{
    public List<INotification> Enqueued { get; } = new();

    public void Enqueue(INotification notification) => Enqueued.Add(notification);
}
