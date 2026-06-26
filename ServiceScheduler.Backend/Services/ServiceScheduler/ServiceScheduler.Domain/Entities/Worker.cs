using ServiceScheduler.Domain.Policies;
using ServiceScheduler.Domain.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Exceptions;

namespace ServiceScheduler.Domain.Entities;

public class Worker : LifeCycleEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Cpf { get; private set; } = string.Empty;

    public ICollection<AvailablePeriod> AvailablePeriods { get; private set; } = new List<AvailablePeriod>();
    public ICollection<UnavailablePeriod> UnavailablePeriods { get; private set; } = new List<UnavailablePeriod>();

    private Worker() { }

    public static Worker Create(
        string name,
        string phone,
        string email,
        string cpf,
        ICollection<AvailablePeriod>? availablePeriods = null,
        ICollection<UnavailablePeriod>? unavailablePeriods = null)
    {
        WorkerPolicy.EnsureNameIsValid(name);
        WorkerPolicy.EnsurePhoneIsValid(phone);
        WorkerPolicy.EnsureEmailIsValid(email);
        WorkerPolicy.EnsureCpfIsValid(cpf);

        availablePeriods ??= new List<AvailablePeriod>();
        unavailablePeriods ??= new List<UnavailablePeriod>();

        WorkerPolicy.EnsureAvailablePeriodsDoNotOverlap(availablePeriods);
        WorkerPolicy.EnsureUnavailablePeriodsDoNotOverlap(unavailablePeriods);

        return new Worker
        {
            Name = name.Trim(),
            Phone = phone.Trim(),
            Email = email.Trim(),
            Cpf = cpf.Trim(),
            AvailablePeriods = availablePeriods,
            UnavailablePeriods = unavailablePeriods
        };
    }

    public void Update(
        string name,
        string phone,
        string email,
        string cpf)
    {
        WorkerPolicy.EnsureNameIsValid(name);
        WorkerPolicy.EnsurePhoneIsValid(phone);
        WorkerPolicy.EnsureEmailIsValid(email);
        WorkerPolicy.EnsureCpfIsValid(cpf);

        Name = name.Trim();
        Phone = phone.Trim();
        Email = email.Trim();
        Cpf = cpf.Trim();

        Touch();
    }

    public void AddAvailablePeriod(AvailablePeriod period)
    {
        WorkerPolicy.EnsureAvailablePeriodCanBeAdded(
            period,
            AvailablePeriods);

        AvailablePeriods.Add(period);

        Touch();
    }

    public void RemoveAvailablePeriod(AvailablePeriod period)
    {
        if (!AvailablePeriods.Remove(period))
            throw new DomainValidationException("Período de disponibilidade não encontrado.");

        Touch();
    }

    public void AddUnavailablePeriod(UnavailablePeriod period)
    {
        WorkerPolicy.EnsureUnavailablePeriodCanBeAdded(
            period,
            UnavailablePeriods);

        UnavailablePeriods.Add(period);

        Touch();
    }

    public void RemoveUnavailablePeriod(UnavailablePeriod period)
    {
        if (!UnavailablePeriods.Remove(period))
            throw new DomainValidationException("Período de indisponibilidade não encontrado.");

        Touch();
    }

    public void PreemptivelyEndCurrentUnavailablePeriod(DateTime endDate)
    {
        var current = UnavailablePeriods.FirstOrDefault(x => x.Start <= DateTime.UtcNow && x.End > DateTime.UtcNow);

        if (current is null)
            throw new DomainValidationException("Não existe um período de indisponibilidade em andamento.");

        if (endDate <= current.Start)
            throw new DomainValidationException("A nova data de término deve ser posterior ao início da indisponibilidade.");

        UnavailablePeriods.Remove(current);

        UnavailablePeriods.Add(
            new UnavailablePeriod(
                current.Start,
                endDate,
                current.Reason));

        Touch();
    }
}
