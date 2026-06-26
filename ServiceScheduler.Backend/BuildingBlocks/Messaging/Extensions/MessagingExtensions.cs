using BuildingBlocks.Messaging.Abstractions;
using BuildingBlocks.Messaging.EasyNetQ;
using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Messaging.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddEasyNetQ(connectionString);
        // Override EasyNetQ's default type name serializer with our short-name router
        services.AddSingleton<ITypeNameSerializer, SimpleTypeNameSerializer>();

        services.AddSingleton<EasyNetQMessageBus>();
        services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<EasyNetQMessageBus>());
        services.AddSingleton<IMessageSubscriber>(sp => sp.GetRequiredService<EasyNetQMessageBus>());

        return services;
    }
}
