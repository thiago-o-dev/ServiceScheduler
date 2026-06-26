using Microsoft.EntityFrameworkCore;
using SharedKernel.Abstractions;

namespace BuildingBlocks.Persistence.EntityFramework;

public abstract class AppDbContext : DbContext
{
    protected AppDbContext(
        DbContextOptions options)
        : base(options)
    {
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents()
    {
        return ChangeTracker
            .Entries<BaseEntity>()
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();
    }

    public void ClearDomainEvents()
    {
        var aggregates = ChangeTracker
            .Entries<BaseEntity>();

        foreach (var aggregate in aggregates)
        {
            aggregate.Entity.ClearDomainEvents();
        }
    }
}