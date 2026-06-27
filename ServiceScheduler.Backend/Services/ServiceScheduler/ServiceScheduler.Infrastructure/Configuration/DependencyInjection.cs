using BuildingBlocks.Messaging.Extensions;
using BuildingBlocks.Persistence.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Infrastructure.Persistence;
using ServiceScheduler.Infrastructure.Persistence.Repositories;
using System;

namespace ServiceScheduler.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("scheduler-db")
            ?? throw new InvalidOperationException("Connection string 'scheduler-db' not found.");

        var rabbitMqConnectionString = configuration.GetConnectionString("rabbitmq")
            ?? throw new InvalidOperationException("Connection string 'rabbitmq' not configured.");

        services.AddPersistence<SchedulerDbContext>(connectionString);
        services.AddMessaging(rabbitMqConnectionString);
        services.AddOutboxWorker<SchedulerDbContext>();

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IWorkerRepository, WorkerRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IServiceBundleRepository, ServiceBundleRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();

        return services;
    }
}
