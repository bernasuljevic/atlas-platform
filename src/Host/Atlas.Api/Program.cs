using Atlas.Api.ExceptionHandling;
using Atlas.Modules.Auth.Api;
using Atlas.Modules.Wiki.Api;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// MODÜL KAYITLARI (Dependency Injection)
// ============================================================
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddWikiModule(builder.Configuration);

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

var app = builder.Build();

app.UseExceptionHandler();

app.UseCors("AllowReactApp");

// Kimlik doğrulama (bu kim?) ve yetkilendirme (bunu yapabilir mi?) middleware'leri.
// Sıra önemli: önce Authentication, sonra Authorization - authorization, kimliği
// henüz bilinmeyen bir isteği değerlendiremez.
app.UseAuthentication();
app.UseAuthorization();

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
