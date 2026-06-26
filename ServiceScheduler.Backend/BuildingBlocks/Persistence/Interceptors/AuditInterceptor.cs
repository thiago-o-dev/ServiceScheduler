using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BuildingBlocks.Persistence.Interceptors;

public sealed class AuditInterceptor
    : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>>
        SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;

        if (dbContext is null)
        {
            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }

        var entries = dbContext.ChangeTracker
            .Entries()
            .Where(x =>
                x.State == EntityState.Added ||
                x.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Properties.Any(x => x.Metadata.Name == "UpdatedAt"))
            {
                entry.Property("UpdatedAt").CurrentValue =
                    DateTime.UtcNow;
            }

            if (entry.State == EntityState.Added &&
                entry.Properties.Any(x => x.Metadata.Name == "CreatedAt"))
            {
                entry.Property("CreatedAt").CurrentValue =
                    DateTime.UtcNow;
            }
        }

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }
}