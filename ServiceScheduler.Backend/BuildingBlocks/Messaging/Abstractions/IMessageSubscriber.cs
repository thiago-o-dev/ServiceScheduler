namespace BuildingBlocks.Messaging.Abstractions;

public interface IMessageSubscriber
{
    Task SubscribeAsync<TMessage>(
        string subscriptionId,
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}
