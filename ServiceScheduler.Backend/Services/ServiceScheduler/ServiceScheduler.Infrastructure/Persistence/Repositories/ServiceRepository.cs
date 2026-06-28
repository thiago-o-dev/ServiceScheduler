using Microsoft.EntityFrameworkCore;
using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Domain.Entities;

namespace ServiceScheduler.Infrastructure.Persistence.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly SchedulerDbContext _context;

    public ServiceRepository(SchedulerDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Service service, CancellationToken cancellationToken = default)
    {
        await _context.Services.AddAsync(service, cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Services.ToListAsync(cancellationToken);
    }

    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _context.Services.Where(s => idList.Contains(s.Id)).ToListAsync(cancellationToken);
    }

    public void Remove(Service service)
    {
        _context.Services.Remove(service);
    }
}
