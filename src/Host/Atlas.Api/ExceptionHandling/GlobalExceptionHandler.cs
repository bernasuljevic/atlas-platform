using FluentValidation;
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
        _logger.LogError(exception, "İşlenmeyen hata yakalandı: {Message}", exception.Message);

        // FluentValidation'ın ValidationBehavior'dan fırlattığı hata, diğerlerinden
        // farklı: alan bazlı birden fazla hata taşıyor, tek bir "Detail" string'ine
        // sığmıyor. Bu yüzden ayrı bir dal - ValidationProblemDetails, "errors" adında
        // { alanAdı: [mesajlar] } şeklinde bir sözlük üretir (React tarafı alan
        // bazlı gösterebilsin diye).
        if (exception is ValidationException validationException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Geçersiz istek"
                }
            });
        }

        // Domain katmanı kural ihlallerini ArgumentException ile fırlatıyor (bkz. WikiPage.Create).
        // Bu yüzden burada "beklenen" bir hata sayılıp 400 dönüyor; geri kalan her şey
        // (beklenmeyen hatalar - null reference, DB koptu vb.) 500 sayılıyor.
        var (statusCode, title) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Geçersiz istek"),
            // Yetki kuralı ihlalleri (örn. başkasının wiki sayfasını silmeye çalışmak) -
            // bir "kötü istek" değil, "kimliğin doğru ama bu işlemi yapamazsın" anlamına
            // geliyor, bu yüzden 400 değil 403 dönüyor.
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Bu işlem için yetkiniz yok"),
            _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu")
        };

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
