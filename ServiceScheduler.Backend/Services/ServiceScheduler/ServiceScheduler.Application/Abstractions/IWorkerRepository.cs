using ServiceScheduler.Domain.Entities;

namespace ServiceScheduler.Application.Abstractions;

public interface IWorkerRepository
{
    Task AddAsync(Worker worker, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Worker>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Worker?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Worker?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Worker?> FindDuplicateAsync(string cpf, string email, CancellationToken cancellationToken = default);
}
