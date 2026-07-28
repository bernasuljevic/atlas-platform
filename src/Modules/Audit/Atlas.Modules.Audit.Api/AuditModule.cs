using Atlas.Modules.Audit.Application.Abstractions;
using Atlas.Modules.Audit.Application.AuditLog.Queries;
using Atlas.Modules.Audit.Infrastructure.Persistence;
using Atlas.Shared.Contracts;
using Atlas.Shared.CQRS.Behaviors;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Modules.Audit.Api;

public static class AuditModule
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Ayrı bir veritabanı DEĞİL - Auth/Wiki ile aynı SQL Server veritabanını
        // (AtlasPlatform), kendi "audit" şemasıyla paylaşıyor (bkz. AuditDbContext).
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("'DefaultConnection' bağlantı dizesi appsettings.json'da bulunamadı.");

        services.AddDbContext<AuditDbContext>(options => options.UseSqlServer(connectionString));

        // IAuditLogWriter (Shared.Contracts) - AuditBehavior (Shared.CQRS) bunu
        // enjekte ediyor, Wiki hangi modülün bunu implemente ettiğini hiç bilmiyor.
        services.AddScoped<IAuditLogWriter, EfAuditLogWriter>();

        services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<GetAuditLogQuery>();
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssemblyContaining<GetAuditLogQuery>();

        return services;
    }

    /// <summary>
    /// Host, AuditDbContext'in varlığından habersiz - sadece bu metodu çağırır
    /// (Auth/Wiki/AI'daki MigrateXDatabase ile aynı desen).
    /// </summary>
    public static void MigrateAuditDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            db.Database.EnsureCreated();
        }
        else
        {
            db.Database.Migrate();
        }
    }
}
