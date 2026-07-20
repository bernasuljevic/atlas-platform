using System.Text;
using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Application.Users.Queries;
using Atlas.Modules.Auth.Infrastructure.CurrentUser;
using Atlas.Modules.Auth.Infrastructure.Persistence;
using Atlas.Modules.Auth.Infrastructure.Security;
using Atlas.Shared.Contracts;
using Microsoft.AspNetCore.Builder;
using Atlas.Shared.CQRS.Behaviors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Atlas.Modules.Auth.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Atlas.Modules.Auth.Domain.Enums;

namespace Atlas.Modules.Auth.Api;

/// <summary>
/// BURASI DOKÜMANDAKİ "Sonraki Adım" ÖNERİSİNİN CEVABI.
///
/// Host projesi (Atlas.Api), Auth modülünün İÇİNDE hangi sınıfların olduğunu,
/// hangi interface'in hangi implementasyona bağlandığını hiç bilmiyor.
/// Sadece "AddAuthModule()" diyor, gerisi burada, modülün kendi sorumluluğunda.
/// </summary>
public static class AuthModule
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("'DefaultConnection' bağlantı dizesi appsettings.json'da bulunamadı.");

        // AuthDbContext, gerçek bir DB bağlantısını (SqlConnection) sarmalıyor -
        // bu yüzden Scoped (AddDbContext'in varsayılanı zaten Scoped'tur).
        services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(connectionString));

        // Artık repository DIŞ BİR KAYNAĞI (DbContext/DB bağlantısı) sarmalıyor,
        // kendisi veri deposu değil - CLAUDE.md'deki kural burada devreye giriyor:
        // Scoped doğru seçim, her HTTP isteği kendi DbContext'ini (ve bağlantısını) alır.
        services.AddScoped<IUserRepository, EfUserRepository>();

        // Shared.Contracts'taki ICurrentUserAccessor'ın gerçek implementasyonu burada
        // bağlanıyor. Wiki modülü bu sınıfı hiç görmüyor - sadece interface'i biliyor.
        // Scoped kullanıyoruz çünkü ileride HttpContext'i saracak - HttpContext gerçekten
        // istek bazlı bir kaynak, o yüzden burada Scoped doğru (geçen dersteki hatanın tersi).
        services.AddHttpContextAccessor();
services.AddScoped<ICurrentUserAccessor, HttpCurrentUserAccessor>();

services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
services.AddScoped<ITokenGenerator, JwtTokenGenerator>();

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
        };
    });
services.AddAuthorization();

        // MediatR'a "Command/Query/Handler'ları GetAllUsersQuery'nin bulunduğu
        // assembly içinde ara" diyoruz - elle tek tek kayıt yapmamıza gerek yok,
        // MediatR o assembly'deki tüm IRequestHandler implementasyonlarını kendisi bulur.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<GetAllUsersQuery>();

            // Her Command/Query bu davranıştan geçsin - loglama burada devreye giriyor.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        return services;
    }

    /// <summary>
    /// Host (Program.cs) sadece bu metodu çağırıyor - AuthDbContext'in var olduğunu
    /// bile bilmiyor. Migration'ları uygular ve tablo boşsa admin kullanıcısını ekler
    /// (InMemoryUserRepository'nin constructor'ında yaptığı seed işleminin karşılığı).
    /// </summary>
    public static void MigrateAuthDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        db.Database.Migrate();

        if (!db.Users.Any())
        {
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var adminPasswordHash = passwordHasher.Hash("Admin123!");

            db.Users.Add(User.Create("admin@atlas.local", "Atlas Admin", adminPasswordHash, UserRole.Admin));
            db.SaveChanges();
        }
    }
}
