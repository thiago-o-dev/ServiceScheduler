using BuildingBlocks.Persistence.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions;

namespace BuildingBlocks.Persistence;

// Resolves IDomainEventHandler<T> registrations from DI and invokes them in-process.
// Used by DomainEventsInterceptor for same-service handlers only.
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
                await (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}
