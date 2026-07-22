using Atlas.Modules.AI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Modules.AI.Api;

public static class AIModule
{
    public static IServiceCollection AddAIModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("'Postgres' bağlantı dizesi appsettings.json'da bulunamadı.");

        // UseVector(): Npgsql'e "vector" PostgreSQL tipini Pgvector.Vector CLR tipiyle
        // eşleştirmesini söylüyor - bu olmadan Npgsql "vector" sütununu tanımaz.
        services.AddDbContext<AiDbContext>(options =>
            options.UseNpgsql(connectionString, o => o.UseVector()));

        services.AddHealthChecks().AddDbContextCheck<AiDbContext>("postgresql");

        return services;
    }

    /// <summary>
    /// Auth/Wiki'deki MigrateAuthDatabase/MigrateWikiDatabase ile aynı desen -
    /// Host, AiDbContext'in varlığını bilmiyor, sadece bu metodu çağırıyor.
    /// </summary>
    public static void MigrateAiDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AiDbContext>();
        db.Database.Migrate();
    }
}
