using SharedKernel.Abstractions;

namespace BuildingBlocks.Persistence.Abstractions; 

public interface IDomainEventDispatcher { 
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default); 
}