using BuildingBlocks.Persistence.Abstractions;
using BuildingBlocks.Persistence.EntityFramework;
using BuildingBlocks.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Persistence.Extensions;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence<TDbContext>(
        this IServiceCollection services,
        string connectionString)
        where TDbContext : AppDbContext
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddScoped<AuditInterceptor>();
        services.AddScoped<OutboxInterceptor>();
        services.AddScoped<DomainEventsInterceptor>();

        services.AddDbContext<TDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);

            options.AddInterceptors(
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<OutboxInterceptor>(),
                sp.GetRequiredService<DomainEventsInterceptor>());
        });

        services.AddScoped<IUnitOfWork, EfUnitOfWork<TDbContext>>();

        return services;
    }

    public static IServiceCollection AddOutboxWorker<TDbContext>(
        this IServiceCollection services)
        where TDbContext : AppDbContext
    {
        services.AddHostedService<OutboxWorker<TDbContext>>();
        return services;
    }
}
