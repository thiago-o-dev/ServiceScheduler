using SharedKernel.Abstractions;
using SharedKernel.Exceptions;

namespace ServiceScheduler.Domain.Entities;

/// <summary>
/// Serviços como corte de cabelo, cilios, manicure...
/// </summary>
public class Service : LifeCycleEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Value { get; private set; } = decimal.Zero;

    private Service() { }

    public static Service Create(string name, string description, decimal value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Nome de serviço é obrigatório.");
        if (value < 0)
            throw new DomainValidationException("Valor de serviço não pode ser menor que 0.");


        return new Service
        {
            Name = name.Trim(),
            Description = description.Trim(),
            Value = value
        };
    }

    public void Update(string name, string description, decimal value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Nome de serviço é obrigatório.");
        if (value < 0)
            throw new DomainValidationException("Valor de serviço não pode ser menor que 0.");

        Name = name.Trim();
        Description = description.Trim();
        Value = value;
        Touch();
    }
}
