using ServiceScheduler.Domain.Policies;
using SharedKernel.Abstractions;
using SharedKernel.Exceptions;

namespace ServiceScheduler.Domain.Entities;

public class Schedule : LifeCycleEntity
{
    public Guid ClientId { get; private set; }
    public Guid WorkerId { get; private set; }
    public ICollection<Guid> ServiceIds { get; private set; } = new List<Guid>();
    public DateTime ScheduledAt { get; private set; }
    public TimeSpan Duration { get; private set; }

    private Schedule() { }

    public static Schedule Create(Guid clientId, Guid workerId, ICollection<Guid> serviceIds, DateTime scheduledAt, TimeSpan duration)
    {
        if (clientId.Equals(Guid.Empty))
            throw new DomainValidationException("Id do Cliente não pode ser vazio");

        if (workerId.Equals(Guid.Empty))
            throw new DomainValidationException("Id do Trabalhador não pode ser vazio");

        SchedulePolicy.EnsureAllServiceIdsAreValid(serviceIds);
        SchedulePolicy.EnsureScheduledAtIsInFuture(scheduledAt);
        SchedulePolicy.EnsureDurationIsWithinAllowedRange(duration);

        var schedule = new Schedule
        {
            ClientId = clientId,
            WorkerId = workerId,
            ServiceIds = serviceIds,
            ScheduledAt = scheduledAt,
            Duration = duration
        };

        return schedule;
    }

    public void AddService(Guid serviceId)
    {
        if (serviceId.Equals(Guid.Empty))
            throw new DomainValidationException("Id do serviço não pode ser vazio");

        ServiceIds.Add(serviceId);
        Touch();
    }

    public void RemoveService(Guid serviceId)
    {
        if (serviceId.Equals(Guid.Empty))
            throw new DomainValidationException("Id do serviço não pode ser vazio");

        ServiceIds.Remove(serviceId);

        Touch();
    }

    public void Update(Guid clientId, Guid workerId, ICollection<Guid> serviceIds, DateTime scheduledAt, TimeSpan duration)
    {
        if (clientId.Equals(Guid.Empty))
            throw new DomainValidationException("Id do Cliente não pode ser vazio");

        if (workerId.Equals(Guid.Empty))
            throw new DomainValidationException("Id do Trabalhador não pode ser vazio");

        SchedulePolicy.EnsureAllServiceIdsAreValid(serviceIds);
        SchedulePolicy.EnsureScheduledAtIsInFuture(scheduledAt);
        SchedulePolicy.EnsureDurationIsWithinAllowedRange(duration);

        ClientId = clientId;
        WorkerId = workerId;
        ServiceIds = serviceIds;
        ScheduledAt = scheduledAt;
        Duration = duration;

        Touch();
    }
}
