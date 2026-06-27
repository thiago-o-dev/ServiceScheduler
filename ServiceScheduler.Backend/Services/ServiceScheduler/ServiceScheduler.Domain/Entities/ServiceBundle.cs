using SharedKernel.Abstractions;
using SharedKernel.Exceptions;

namespace ServiceScheduler.Domain.Entities;

public class ServiceBundle : LifeCycleEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ICollection<Guid> ServiceIds { get; private set; } = new List<Guid>();
    public decimal Value { get; private set; }

    private ServiceBundle() { }

    public static ServiceBundle Create(string name, string description, ICollection<Guid> serviceIds, decimal value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Nome do pacote é obrigatório.");

        if (serviceIds == null || serviceIds.Count < 1)
            throw new DomainValidationException("Um pacote deve conter pelo menos 1 serviço para gerar desconto.");

        if (serviceIds.Any(id => id == Guid.Empty))
            throw new DomainValidationException("Nenhum ID de serviço pode ser vazio.");

        if (value < 0)
            throw new DomainValidationException("Preço do pacote não pode ser menor que 0.");

        return new ServiceBundle
        {
            Name = name.Trim(),
            Description = description.Trim(),
            ServiceIds = serviceIds,
            Value = value
        };
    }

    public void Update(string name, string description, ICollection<Guid> serviceIds, decimal value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Nome do pacote é obrigatório.");

        if (serviceIds == null || serviceIds.Count < 1)
            throw new DomainValidationException("Um pacote deve conter pelo menos 1 serviço para gerar desconto.");

        if (serviceIds.Any(id => id == Guid.Empty))
            throw new DomainValidationException("Nenhum ID de serviço pode ser vazio.");

        if (value < 0)
            throw new DomainValidationException("Preço do pacote não pode ser menor que 0.");

        Name = name.Trim();
        Description = description.Trim();
        ServiceIds = serviceIds;
        Value = value;
        Touch();
    }
}
