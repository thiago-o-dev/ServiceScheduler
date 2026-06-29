namespace ServiceScheduler.Api.Requests.Admin;

public sealed record AdminUpdateScheduleRequest(
    Guid CustomerId,
    Guid WorkerId,
    Guid[] ServiceIds,
    DateTime ScheduledAt,
    TimeSpan Duration,
    decimal? OverrideNetValue
);

