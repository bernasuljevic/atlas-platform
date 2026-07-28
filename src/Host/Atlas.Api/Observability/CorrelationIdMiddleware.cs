using Serilog.Context;

namespace Atlas.Api.Observability;

/// <summary>
/// Her isteğe bir correlation ID kazandırıyor - istemci `X-Correlation-Id`
/// header'ıyla kendi ID'sini gönderirse onu kullanıyoruz (başka bir servisten
/// gelen bir zincirin parçasıysa izlenebilirlik kopmasın diye), yoksa yeni
/// bir tane üretiyoruz. Yanıta da aynı header'la geri yazılıyor - istemci
/// (React tarafı, Swagger, curl) "bu isteğin ID'si neydi" diye response
/// header'ından öğrenebiliyor.
///
/// PIPELINE'DA EN BAŞTA olması kritik: LogContext.PushProperty ile eklenen
/// bu değer, `using` bloğu boyunca (yani isteğin geri kalanı - Authentication,
/// Exception Handling, MediatR'ın LoggingBehavior'ı, EF Core sorguları dahil)
/// atılan HER log satırına otomatik ekleniyor - o log satırlarını üreten
/// kodların CorrelationId'den haberi bile yok, sadece "ambient" context
/// üzerinden geliyor.
/// </summary>
public static class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
                && !string.IsNullOrWhiteSpace(existing)
                ? existing.ToString()
                : Guid.NewGuid().ToString();

            context.Response.Headers[HeaderName] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await next();
            }
        });
    }
}
