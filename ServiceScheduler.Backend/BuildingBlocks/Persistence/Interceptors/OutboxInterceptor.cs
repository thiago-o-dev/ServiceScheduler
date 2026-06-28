using BuildingBlocks.Persistence.EntityFramework;
using BuildingBlocks.Persistence.Models;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SharedKernel.Abstractions;
using System.Text.Json;

namespace BuildingBlocks.Persistence.Interceptors;

public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>>
        SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not AppDbContext dbContext)
        {
            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }

        var outboxMessages = dbContext
            .ChangeTracker
            .Entries<BaseEntity>()
            .SelectMany(x => x.Entity.DomainEvents)
            .Select(domainEvent => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = domainEvent.GetType().Name,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                OccurredOnUtc = DateTime.UtcNow
            })
            .ToList();

        dbContext.Set<OutboxMessage>()
            .AddRange(outboxMessages);

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }
}
