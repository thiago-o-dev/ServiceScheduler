namespace ServiceScheduler.Api.Requests.Schedule;

public sealed record UpdateScheduleRequest(Guid WorkerId, Guid[] ServiceIds, DateTime ScheduledAt, TimeSpan Duration);