using Microsoft.EntityFrameworkCore;
using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceScheduler.Infrastructure.Persistence.Repositories;

public class WorkerRepository : IWorkerRepository
{
    private readonly SchedulerDbContext _context;

    public WorkerRepository(SchedulerDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Worker worker, CancellationToken cancellationToken = default)
    {
        await _context.Workers.AddAsync(worker, cancellationToken);
    }

    public async Task<IReadOnlyList<Worker>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Workers
            .Include(w => w.AvailablePeriods)
            .Include(w => w.UnavailablePeriods)
            .ToListAsync(cancellationToken);
    }

    public async Task<Worker?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Workers
            .Include(w => w.AvailablePeriods)
            .Include(w => w.UnavailablePeriods)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<Worker?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Workers
            .Include(w => w.AvailablePeriods)
            .Include(w => w.UnavailablePeriods)
            .FirstOrDefaultAsync(w => w.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Workers.AnyAsync(w => w.Email == email, cancellationToken);
    }
}
