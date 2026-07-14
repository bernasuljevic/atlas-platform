using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Application.WikiPages.Queries;
using Atlas.Modules.Wiki.Infrastructure.Persistence;
using Atlas.Shared.CQRS.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Modules.Wiki.Api;

public static class WikiModule
{
    public static IServiceCollection AddWikiModule(this IServiceCollection services)
    {
        // Singleton - InMemoryWikiPageRepository kendisi veri deposu (bkz. sınıfın içindeki not).
        services.AddSingleton<IWikiPageRepository, InMemoryWikiPageRepository>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<GetWikiPagesQuery>();
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        return services;
    }
}
