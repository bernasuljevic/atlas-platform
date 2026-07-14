using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Domain katmanı kural ihlallerini ArgumentException ile fırlatıyor (bkz. WikiPage.Create).
        // Bu yüzden burada "beklenen" bir hata sayılıp 400 dönüyor; geri kalan her şey
        // (beklenmeyen hatalar - null reference, DB koptu vb.) 500 sayılıyor.
        var (statusCode, title) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Geçersiz istek"),
            _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu")
        };

        _logger.LogError(exception, "İşlenmeyen hata yakalandı: {Message}", exception.Message);

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                // 500'lerde exception mesajını client'a sızdırma - sadece loglanır.
                Detail = statusCode == StatusCodes.Status500InternalServerError ? null : exception.Message
            }
        });
    }
}
