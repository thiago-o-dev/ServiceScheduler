using ServiceScheduler.Domain.Policies;
using SharedKernel.Abstractions;
using SharedKernel.Exceptions;

namespace ServiceScheduler.Domain.Entities;

public class Schedule : LifeCycleEntity
{
    public Guid CustomerId { get; private set; }
    public Guid WorkerId { get; private set; }
    public ICollection<ScheduledService> Services { get; private set; } = new List<ScheduledService>();
    public DateTime ScheduledAt { get; private set; }
    public TimeSpan Duration { get; private set; }
    public decimal BruteValue { get; private set; }
    public decimal? CustomNetValue { get; private set; }
    public ScheduleStatus Status { get; private set; } = ScheduleStatus.Pending;

    public ICollection<Guid> ServiceIds => Services.Select(s => s.ServiceId).ToList();

    private Schedule() { }

    public static Schedule Create(Guid customerId, Guid workerId, ICollection<Service> services, DateTime scheduledAt, TimeSpan duration)
    {
        if (customerId.Equals(Guid.Empty))
            throw new DomainValidationException("Id do Customer não pode ser vazio");

        if (workerId.Equals(Guid.Empty))
            throw new DomainValidationException("Id do Trabalhador não pode ser vazio");

        if (services == null || !services.Any())
            throw new DomainValidationException("Ao menos um serviço deve ser selecionado.");

        var serviceIds = services.Select(s => s.Id).ToList();
        SchedulePolicy.EnsureAllServiceIdsAreValid(serviceIds);
        SchedulePolicy.EnsureScheduledAtIsInFuture(scheduledAt);
        SchedulePolicy.EnsureDurationIsWithinAllowedRange(duration);

        var bruteValue = services.Sum(s => s.Value);

        var schedule = new Schedule
        {
            CustomerId = customerId,
            WorkerId = workerId,
            Services = services.Select(s => new ScheduledService(s.Id, ServiceStatus.Pending)).ToList(),
            ScheduledAt = scheduledAt,
            Duration = duration,
            BruteValue = bruteValue,
            Status = ScheduleStatus.Pending
        };

        return schedule;
    }

    public void AddService(Service service)
    {
        if (service == null)
            throw new DomainValidationException("Serviço não pode ser nulo.");

        if (service.Id.Equals(Guid.Empty))
            throw new DomainValidationException("Id do serviço não pode ser vazio");

        if (Services.Any(s => s.ServiceId == service.Id))
            throw new DomainValidationException("Serviço já adicionado a este agendamento.");

        Services.Add(new ScheduledService(service.Id, ServiceStatus.Pending));
        BruteValue += service.Value;
        Touch();
    }

    public void RemoveService(Service service)
    {
        if (service == null)
            throw new DomainValidationException("Serviço não pode ser nulo.");

        var existing = Services.FirstOrDefault(s => s.ServiceId == service.Id);
        if (existing != null && Services.Remove(existing))
        {
            BruteValue -= service.Value;
        }

        Touch();
    }

    public void UpdateServices(ICollection<Service> services)
    {
        if (services == null || !services.Any())
            throw new DomainValidationException("Ao menos um serviço deve ser selecionado.");

        var serviceIds = services.Select(s => s.Id).ToList();
        SchedulePolicy.EnsureAllServiceIdsAreValid(serviceIds);

        // Keep existing statuses if services are still present, otherwise create new
        var newServices = new List<ScheduledService>();
        foreach (var s in services)
        {
            var existing = Services.FirstOrDefault(es => es.ServiceId == s.Id);
            if (existing != null)
            {
                newServices.Add(existing);
            }
            else
            {
                newServices.Add(new ScheduledService(s.Id, ServiceStatus.Pending));
            }
        }

        Services = newServices;
        BruteValue = services.Sum(s => s.Value);

        Touch();
    }

    public void Update(Guid customerId, Guid workerId, DateTime scheduledAt, TimeSpan duration)
    {
        if (customerId.Equals(Guid.Empty))
            throw new DomainValidationException("Id do Customer não pode ser vazio");

        if (workerId.Equals(Guid.Empty))
            throw new DomainValidationException("Id do Trabalhador não pode ser vazio");

        SchedulePolicy.EnsureScheduledAtIsInFuture(scheduledAt);
        SchedulePolicy.EnsureDurationIsWithinAllowedRange(duration);

        CustomerId = customerId;
        WorkerId = workerId;
        ScheduledAt = scheduledAt;
        Duration = duration;

        Touch();
    }

    public void Confirm()
    {
        if (Status == ScheduleStatus.Canceled)
            throw new DomainValidationException("Não é possível confirmar um agendamento cancelado.");

        Status = ScheduleStatus.Confirmed;
        foreach (var service in Services)
        {
            if (service.Status == ServiceStatus.Pending)
            {
                service.UpdateStatus(ServiceStatus.Confirmed);
            }
        }
        Touch();
    }

    public void Cancel()
    {
        Status = ScheduleStatus.Canceled;
        foreach (var service in Services)
        {
            service.UpdateStatus(ServiceStatus.Canceled);
        }
        Touch();
    }

    public void UpdateServiceStatus(Guid serviceId, ServiceStatus status)
    {
        var service = Services.FirstOrDefault(s => s.ServiceId == serviceId);
        if (service == null)
            throw new DomainValidationException("Serviço não encontrado neste agendamento.");

        service.UpdateStatus(status);
        Touch();
    }

    public void SetCustomNetValue(decimal? value)
    {
        if (value.HasValue && value.Value < 0)
            throw new DomainValidationException("O valor líquido customizado não pode ser menor que 0.");

        CustomNetValue = value;
        Touch();
    }
}
