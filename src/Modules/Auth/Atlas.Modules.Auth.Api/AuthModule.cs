using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Application.Users.Queries;
using Atlas.Modules.Auth.Infrastructure.CurrentUser;
using Atlas.Modules.Auth.Infrastructure.Persistence;
using Atlas.Shared.Contracts;
using Atlas.Shared.CQRS.Behaviors;
using Microsoft.Extensions.DependencyInjection;

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
    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        // Application katmanındaki interface'i (IUserRepository), Infrastructure'daki
        // gerçek implementasyona (InMemoryUserRepository) burada bağlıyoruz.
        // NOT: Burada Singleton kullanıyoruz, ama bu genel kural değil.
        // Genel kural: repository'ler Scoped olur (her istek kendi veritabanı
        // bağlantısını alır). Ama InMemoryUserRepository BAĞLANTI SARMALAMIYOR -
        // kendisi doğrudan verinin deposu (ConcurrentDictionary instance alanı).
        // Scoped olsaydı her istekte yepyeni, boş bir dictionary yaratılır, eklenen
        // her kullanıcı bir sonraki istekte kaybolurdu (tam da yaşadığımız bug).
        // EF Core ile gerçek repository'e geçtiğimizde bu tekrar Scoped olacak,
        // çünkü o zaman DbContext gerçek bir bağlantıyı sarmalayacak.
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();

        // Shared.Contracts'taki ICurrentUserAccessor'ın gerçek implementasyonu burada
        // bağlanıyor. Wiki modülü bu sınıfı hiç görmüyor - sadece interface'i biliyor.
        // Scoped kullanıyoruz çünkü ileride HttpContext'i saracak - HttpContext gerçekten
        // istek bazlı bir kaynak, o yüzden burada Scoped doğru (geçen dersteki hatanın tersi).
        services.AddScoped<ICurrentUserAccessor, FakeCurrentUserAccessor>();

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
}
