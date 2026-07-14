using MediatR;
using Microsoft.Extensions.Logging;

namespace Atlas.Shared.CQRS.Behaviors;

/// <summary>
/// BU SINIF DOKÜMANDAKİ "Shared.CQRS'te sadece ortak pipeline behavior'lar
/// (validation, logging, performance tracking)" satırının somut karşılığı.
///
/// EN ÖNEMLİ KISIM: Bu sınıf tek bir modülü bilmiyor. "IUserRepository" yok,
/// "GetAllUsersQuery" yok - sadece "herhangi bir TRequest, herhangi bir TResponse
/// döndürsün" diyor (generic). Bu sayede Wiki modülü gelince onun Command/Query'leri
/// de otomatik olarak bu loglamadan geçecek, hiçbir ekstra kod yazmadan.
///
/// Çalışma sırası: İstek gelir → BU SINIF çalışır (öncesi) → asıl Handler çalışır →
/// BU SINIF tekrar çalışır (sonrası, "next()" dönünce) → yanıt geri gider.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("[CQRS] {RequestName} başladı", requestName);

        // "next()" çağrısı, zincirdeki bir sonraki adımı (başka bir behavior varsa onu,
        // yoksa asıl Handler'ı) tetikler. Bu satırdan önce yazdığın kod "öncesi",
        // sonrası yazdığın kod "sonrası" çalışır.
        var response = await next();

        _logger.LogInformation("[CQRS] {RequestName} tamamlandı", requestName);

        return response;
    }
}
