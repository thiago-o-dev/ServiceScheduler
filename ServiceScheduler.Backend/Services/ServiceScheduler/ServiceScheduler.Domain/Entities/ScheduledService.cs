using System;

namespace ServiceScheduler.Domain.Entities;

public class ScheduledService
{
    public Guid ServiceId { get; private set; }
    public ServiceStatus Status { get; private set; }

    private ScheduledService() { }

    public ScheduledService(Guid serviceId, ServiceStatus status = ServiceStatus.Pending)
    {
        if (serviceId == Guid.Empty)
            throw new ArgumentException("Service ID cannot be empty", nameof(serviceId));

        ServiceId = serviceId;
        Status = status;
    }

    public void UpdateStatus(ServiceStatus status)
    {
        Status = status;
    }
}
