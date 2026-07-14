using Atlas.Api.ExceptionHandling;
using Atlas.Modules.Auth.Api;
using Atlas.Modules.Wiki.Api;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// MODÜL KAYITLARI (Dependency Injection)
// ============================================================
builder.Services.AddAuthModule();
builder.Services.AddWikiModule();

// Global exception handling - yakalanmamış her hata GlobalExceptionHandler'a düşer,
// ProblemDetails (RFC 7807) formatında JSON döner.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

// ============================================================
// MODÜL ENDPOINT KAYITLARI
// ============================================================
app.MapAuthEndpoints();
app.MapWikiEndpoints();

// Basit bir sağlık kontrolü - "API gerçekten ayakta mı?" sorusuna cevap
app.MapGet("/", () => Results.Ok(new
{
    status = "Atlas API çalışıyor",
    time = DateTime.UtcNow
}))
.WithName("HealthCheck");

app.Run();
