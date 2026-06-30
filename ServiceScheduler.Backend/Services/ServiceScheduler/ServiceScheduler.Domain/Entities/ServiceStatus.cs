namespace ServiceScheduler.Domain.Entities;

public enum ServiceStatus
{
    Pending = 0,
    Confirmed = 1,
    Executing = 2,
    Concluded = 3,
    Cancelled = 4
}
