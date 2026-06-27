using ServiceScheduler.Domain.Entities;

namespace ServiceScheduler.Application.Abstractions;

public interface IClientRepository
{
    Task AddAsync(Client client, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Client?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Client?> FindDuplicateAsync(string cpf, string email, CancellationToken cancellationToken = default);
}
