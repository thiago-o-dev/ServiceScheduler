using Microsoft.EntityFrameworkCore;
using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceScheduler.Infrastructure.Persistence.Repositories;

public class ServiceBundleRepository : IServiceBundleRepository
{
    private readonly SchedulerDbContext _context;

    public ServiceBundleRepository(SchedulerDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ServiceBundle bundle, CancellationToken cancellationToken = default)
    {
        await _context.ServiceBundles.AddAsync(bundle, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceBundle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ServiceBundles.ToListAsync(cancellationToken);
    }

    public async Task<ServiceBundle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceBundles.FirstOrDefaultAsync(sb => sb.Id == id, cancellationToken);
    }

    public void Remove(ServiceBundle bundle)
    {
        _context.ServiceBundles.Remove(bundle);
    }
}
