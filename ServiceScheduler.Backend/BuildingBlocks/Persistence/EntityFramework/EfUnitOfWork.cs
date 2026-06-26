using BuildingBlocks.Persistence.Abstractions;

namespace BuildingBlocks.Persistence.EntityFramework;

public sealed class EfUnitOfWork<TDbContext>
    : IUnitOfWork
    where TDbContext : AppDbContext
{
    private readonly TDbContext _dbContext;

    public EfUnitOfWork(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}