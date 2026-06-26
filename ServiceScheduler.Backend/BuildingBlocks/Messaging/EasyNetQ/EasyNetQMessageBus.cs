using BuildingBlocks.Messaging.Abstractions;
using EasyNetQ;

namespace BuildingBlocks.Messaging.EasyNetQ;

public sealed class EasyNetQMessageBus(IBus bus) : IMessagePublisher, IMessageSubscriber
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
        => bus.PubSub.PublishAsync(message, cancellationToken);

    public Task SubscribeAsync<TMessage>(
        string subscriptionId,
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
        => bus.PubSub.SubscribeAsync<TMessage>(subscriptionId, handler, _ => { }, cancellationToken);
}
