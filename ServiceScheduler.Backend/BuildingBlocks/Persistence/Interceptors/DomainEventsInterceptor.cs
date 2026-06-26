using BuildingBlocks.Persistence.Abstractions;
using BuildingBlocks.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BuildingBlocks.Persistence.Interceptors;

public sealed class DomainEventsInterceptor
    : SaveChangesInterceptor
{
    private readonly IDomainEventDispatcher _dispatcher;

    public DomainEventsInterceptor(
        IDomainEventDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not AppDbContext dbContext)
        {
            return result;
        }

        var domainEvents = dbContext.GetDomainEvents();

        await _dispatcher.DispatchAsync(
            domainEvents,
            cancellationToken);

        dbContext.ClearDomainEvents();

        return result;
    }
}
