using ServiceScheduler.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceScheduler.Application.Abstractions;

public interface IServiceBundleRepository
{
    Task AddAsync(ServiceBundle bundle, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceBundle>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServiceBundle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Remove(ServiceBundle bundle);
}
