using Microsoft.EntityFrameworkCore;
using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Domain.Entities;

namespace ServiceScheduler.Infrastructure.Persistence.Repositories;

public class ScheduleRepository : IScheduleRepository
{
    private readonly SchedulerDbContext _context;

    public ScheduleRepository(SchedulerDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Schedule schedule, CancellationToken cancellationToken = default)
    {
        await _context.Schedules.AddAsync(schedule, cancellationToken);
    }

    public async Task<IReadOnlyList<Schedule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Schedules
            .Include(s => s.Services)
            .ToListAsync(cancellationToken);
    }

    public async Task<Schedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Schedules
            .Include(s => s.Services)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Schedule>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Schedules
            .Include(s => s.Services)
            .Where(s => s.CustomerId == customerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Schedule>> GetByWorkerIdAsync(Guid workerId, CancellationToken cancellationToken = default)
    {
        return await _context.Schedules
            .Include(s => s.Services)
            .Where(s => s.WorkerId == workerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Schedule>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        return await _context.Schedules
            .Include(s => s.Services)
            .Where(s => s.ScheduledAt >= start && s.ScheduledAt <= end)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Schedule>> GetByWeekAsync(Guid customerId, DateTime date, CancellationToken cancellationToken = default)
    {
        // Calculate the start and end date of the week (Monday to Sunday) containing `date`
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        var startOfWeek = date.AddDays(-1 * diff).Date;
        var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);

        return await _context.Schedules
            .Include(s => s.Services)
            .Where(s => s.CustomerId == customerId && s.ScheduledAt >= startOfWeek && s.ScheduledAt <= endOfWeek && s.Status != ScheduleStatus.Canceled)
            .ToListAsync(cancellationToken);
    }

    public void Remove(Schedule schedule)
    {
        _context.Schedules.Remove(schedule);
    }
}
