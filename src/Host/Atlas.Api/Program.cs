using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Atlas.Api.ExceptionHandling;
using Atlas.Api.Observability;
using Atlas.Modules.AI.Api;
using Atlas.Modules.Audit.Api;
using Atlas.Modules.Auth.Api;
using Atlas.Modules.Documents.Api;
using Atlas.Modules.Notifications.Api;
using Atlas.Modules.Vault.Api;
using Atlas.Modules.Wiki.Api;
using Atlas.Shared.Caching;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog, varsayılan Microsoft.Extensions.Logging'in yerini alıyor -
// yapılandırılmış (structured) loglama + CorrelationIdMiddleware'in
// LogContext.PushProperty ile eklediği değerleri otomatik yakalayabilme
// (Enrich.FromLogContext()) için gerekli. Konsol çıktısına CorrelationId'yi
// de basan bir şablon kullanıyoruz - appsettings üzerinden yapılandırmıyoruz,
// tek ortam (Development) için kod içi yapılandırma yeterli, ekstra bir
// appsettings şeması eklemek şimdilik gereksiz karmaşıklık olurdu.
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}");
});

// ============================================================
// MODÜL KAYITLARI (Dependency Injection)
// ============================================================
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddWikiModule(builder.Configuration);
builder.Services.AddCaching(builder.Configuration);
builder.Services.AddNotificationsModule(builder.Configuration);
builder.Services.AddAIModule(builder.Configuration);
builder.Services.AddAuditModule(builder.Configuration);
builder.Services.AddVaultModule(builder.Configuration);
// P3 Gün 2 - henüz sadece storage+persistence bağlı, endpoint YOK (bkz.
// DocumentsModule.cs'teki not).
builder.Services.AddDocumentsModule(builder.Configuration);

// CORS: React uygulamasının (farklı port, localhost:5173) bu API'ye (localhost:5080)
// istek atabilmesi için tarayıcıya "bu adrese izin var" demeliyiz - yoksa tarayıcı
// güvenlik gereği isteği kendisi engeller.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Global exception handling - yakalanmamış her hata GlobalExceptionHandler'a düşer,
// ProblemDetails (RFC 7807) formatında JSON döner.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Rate limiting - iki politika, ikisi de İSTEK BAŞINA DEĞİL, bir ANAHTARA
// (partition) göre sayaç tutuyor, aksi halde tüm kullanıcılar TEK bir ortak
// sayacı paylaşırdı (biri limiti doldurunca herkes engellenirdi).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Varsayılan 429 yanıtı boş gövdeyle dönüyor - ProblemDetails formatına
    // (projenin geri kalanıyla tutarlı) uygun, anlaşılır bir gövde yazıyoruz.
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.20",
            title = "Çok fazla istek",
            status = StatusCodes.Status429TooManyRequests,
            detail = "Kısa süre içinde çok fazla istek gönderdiniz - lütfen biraz bekleyip tekrar deneyin."
        }, cancellationToken);
    };

    // IP bazlı - brute-force şifre denemesine karşı. Kullanıcı bazlı olamaz
    // çünkü login sırasında henüz kimlik bilinmiyor (tam da doğrulanmaya
    // çalışılan şey bu).
    options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    // Kullanıcı bazlı (JWT'deki NameIdentifier) - embedding çağrısı + vector
    // arama içerdiği için "ucuz" bir endpoint değil, gerçek bir embedding
    // sağlayıcısına geçilince (şu an sahte) maliyeti daha da artacak.
    options.AddPolicy("ai-search", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    // "login" ile AYNI gerekçe (IP bazlı, kullanıcı henüz giriş yapmamış) -
    // 6 haneli kodun (1.000.000 olasılık) kaba kuvvetle denenmesine karşı.
    // Dakikada 10 deneme + kodun kendisi zaten 10 dakikada süresi doluyor
    // (bkz. EmailVerificationCode) - ikisi birlikte pratik bir brute-force'u
    // imkansız kılıyor (10dk'da en fazla 100 deneme, 1.000.000'un çok altında).
    options.AddPolicy("email-verification", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    // "email-verification"dan AYRI, çok daha sıkı bir sınır - amaç farklı:
    // orada kod TAHMİNİNİ zorlaştırmak, burada bir kullanıcının gerçek gelen
    // kutusunun "yeniden gönder"le spam'lenmesini engellemek. IP yerine e-posta
    // bazlı - aynı kişi farklı IP'lerden denese bile aynı gelen kutusu hedefleniyor.
    options.AddPolicy("resend-verification", httpContext =>
    {
        // Body'yi burada okumak (rate limiter, endpoint çalışmadan ÖNCE devreye
        // girdiği için) pratik değil - IP bazlı bir kısıtlama yeterli bir ilk
        // savunma hattı, e-posta bazlı bir kısıtlama istenirse ileride
        // EnableBuffering ile body'yi elle okumak gerekirdi (şimdilik YAGNI).
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    // P7 (güvenlik sertleştirme) - Reveal, sertleştirme öncesi HİÇ rate-limit'siz
    // olan tek yazma-dışı yan etkili Vault eylemiydi (login/ai-search/email-
    // verification'ın hepsinde vardı, gerçek bir boşluktu). Kullanıcı bazlı
    // (JWT NameIdentifier) - "ai-search" ile AYNI gerekçe (giriş yapılmış
    // kullanıcı, IP bazlı olmaya gerek yok). Amaç maliyet DEĞİL, VERİ
    // SIZINTISI riski: çalınmış bir token'la kısa sürede Vault'taki TÜM
    // parolaları art arda "reveal" edip toplu dışarı çıkarmayı zorlaştırmak -
    // dakikada 10, normal kullanımda (birkaç parola art arda kontrol etmek)
    // rahatça yeterli ama toplu bir "hepsini dök" script'ini engelliyor.
    options.AddPolicy("vault-reveal", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// Swagger/OpenAPI - her modülün MediatR ile MapGet/MapPost dediği minimal API
// endpoint'lerini otomatik keşfedip belgeliyor. SignalR Hub (Notifications)
// bir REST endpoint'i olmadığı için Swagger'da zaten görünmüyor.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Atlas Platform API", Version = "v1" });

    // Swagger UI'daki "Authorize" butonu - buraya "Bearer eyJ..." yapıştırınca
    // her istekte Authorization header'ı otomatik ekleniyor, elle Postman'e
    // geçmeye gerek kalmıyor.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT access token'ı 'Bearer {token}' formatında gönder - örn. login'den dönen accessToken."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// EN BAŞTA olmalı - bundan sonraki HER middleware'in (Exception Handler dahil)
// attığı loglar aynı CorrelationId'yi otomatik taşısın diye (bkz.
// CorrelationIdMiddleware'deki not). UseSerilogRequestLogging, isteğin
// sonunda (Exception Handler dahil her şey bittikten sonra) method/path/
// status/süre içeren TEK bir özet log satırı basıyor - EF Core'un ayrıntılı
// sorgu loglarının yanına, "bu istek genel olarak ne yaptı" sorusuna hızlı
// cevap veren bir üst seviye satır ekliyor.
app.UseCorrelationId();
app.UseSerilogRequestLogging();

app.UseExceptionHandler();

// /swagger -> UI, /swagger/v1/swagger.json -> ham OpenAPI belgesi.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowReactApp");

// Kimlik doğrulama (bu kim?) ve yetkilendirme (bunu yapabilir mi?) middleware'leri.
// Sıra önemli: önce Authentication, sonra Authorization - authorization, kimliği
// henüz bilinmeyen bir isteği değerlendiremez.
app.UseAuthentication();
app.UseAuthorization();

// UseAuthorization'DAN SONRA olmalı - "ai-search" politikası HttpContext.User'ı
// (JWT'den doldurulan) okuyor, bu ancak Authentication/Authorization
// çalıştıktan sonra dolu oluyor.
app.UseRateLimiter();

// ============================================================
// VERİTABANI MIGRATION'LARI
// ============================================================
// Her modül kendi migration'ını kendi uyguluyor - Host, AuthDbContext/WikiDbContext'in
// var olduğunu bilmiyor, sadece "veritabanını hazırla" diye modüle sesleniyor.
// NOT: Uygulama her açılışta migration'ı otomatik uyguluyor - küçük bir öğrenme projesi
// için pratik, ama gerçek prod ortamında (özellikle aynı anda birden fazla instance
// ayağa kalkarsa) migration'lar genelde ayrı bir deploy adımı olarak elle çalıştırılır.
app.MigrateAuthDatabase();
app.MigrateWikiDatabase();
app.MigrateAiDatabase();
app.MigrateAuditDatabase();
app.MigrateVaultDatabase();
app.MigrateDocumentsDatabase();

// ============================================================
// MODÜL ENDPOINT KAYITLARI
// ============================================================
app.MapAuthEndpoints();
app.MapWikiEndpoints();
app.MapNotificationsEndpoints();
app.MapAIEndpoints();
app.MapAuditEndpoints();
app.MapVaultEndpoints();
// P3 Gün 3 - şimdilik sadece upload; liste/detay/indirme Gün 4'te gelecek.
app.MapDocumentsEndpoints();

// Basit bir sağlık kontrolü - "API gerçekten ayakta mı?" sorusuna cevap
app.MapGet("/", () => Results.Ok(new
{
    status = "Atlas API çalışıyor",
    time = DateTime.UtcNow
}))
.WithName("HealthCheck");

// "/" endpoint'i sadece API sürecinin ayakta olduğunu söylüyor - "/health" ise
// gerçekten bağımlılıklara (SQL Server, Redis, PostgreSQL) ulaşılabiliyor mu diye
// bakıyor. Her check kaydı ilgili modülün AddXModule()/AddCaching() metodunda -
// Host burada sadece sonucu sade bir JSON'a yazan endpoint'i map'liyor.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            services = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.Status.ToString())
        });

        await context.Response.WriteAsync(json);
    }
});

app.Run();

// Top-level statement'lar arka planda "Program" adında bir sınıf üretir ama bu
// sınıf varsayılan olarak internal'dır - integration test projesindeki
// WebApplicationFactory<Program>'ın buraya erişebilmesi için public yapıyoruz.
public partial class Program;
