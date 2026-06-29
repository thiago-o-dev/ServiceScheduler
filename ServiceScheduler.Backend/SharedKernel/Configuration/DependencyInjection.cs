using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.Implementations.CQRS;

namespace SharedKernel.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddRequestDispatcher(
        this IServiceCollection services)
    {
        services.AddScoped<IRequestDispatcher, RequestDispatcher>();

        return services;
    }
}