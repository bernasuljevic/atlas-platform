using System.Net;

namespace Atlas.Modules.AI.Infrastructure.Embeddings;

/// <summary>
/// VoyageEmbeddingService'in fırlattığı tek özel istisna türü - hem HTTP hata
/// yanıtlarını (429/5xx/4xx) hem "boyut beklenenle uyuşmuyor" gibi sözleşme
/// ihlallerini taşıyor. StatusCode, retry mantığının "bu geçici mi kalıcı mı"
/// kararını verebilmesi için tutuluyor.
/// </summary>
public class VoyageEmbeddingException : Exception
{
    public HttpStatusCode? StatusCode { get; }

    public VoyageEmbeddingException(string message, HttpStatusCode? statusCode = null) : base(message)
    {
        StatusCode = statusCode;
    }
}
