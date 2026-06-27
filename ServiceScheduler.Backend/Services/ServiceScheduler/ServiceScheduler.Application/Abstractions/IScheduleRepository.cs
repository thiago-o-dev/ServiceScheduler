using ServiceScheduler.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceScheduler.Application.Abstractions;

public interface IScheduleRepository
{
    Task AddAsync(Schedule schedule, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Schedule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Schedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Schedule>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Schedule>> GetByWorkerIdAsync(Guid workerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Schedule>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Schedule>> GetByWeekAsync(Guid customerId, DateTime date, CancellationToken cancellationToken = default);
    void Remove(Schedule schedule);
}
