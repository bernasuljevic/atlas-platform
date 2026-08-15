using System.Net;
using System.Net.Http.Json;

namespace Atlas.Modules.AI.Infrastructure.Tests.Fakes;

// Mocking kütüphanesi kullanmadan HttpClient'ı test edebilmenin standart yolu:
// gerçek ağa hiç çıkmayan, çağrıldıkça bir kuyruktan sırayla cevap veren bir
// HttpMessageHandler. VoyageEmbeddingService'in retry mantığını test edebilmek
// için (ör. "ilk çağrı 429, ikinci çağrı başarılı") birden fazla cevap
// sıraya konulabiliyor.
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses = new();
    public List<HttpRequestMessage> Requests { get; } = new();

    public FakeHttpMessageHandler EnqueueJson(HttpStatusCode statusCode, object body)
    {
        _responses.Enqueue(() => new HttpResponseMessage(statusCode) { Content = JsonContent.Create(body) });
        return this;
    }

    public FakeHttpMessageHandler EnqueueError(HttpStatusCode statusCode, string body = "hata")
    {
        _responses.Enqueue(() => new HttpResponseMessage(statusCode) { Content = new StringContent(body) });
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (_responses.Count == 0)
            throw new InvalidOperationException("FakeHttpMessageHandler: sırada bekleyen bir cevap yok - test yeterince EnqueueJson/EnqueueError çağırmamış olabilir.");

        return Task.FromResult(_responses.Dequeue()());
    }
}
