using ServiceScheduler.Domain.Entities;

namespace ServiceScheduler.Application.Abstractions;

public interface IServiceRepository
{
    Task AddAsync(Service service, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    void Remove(Service service);
}
