namespace ServiceScheduler.Api.Requests.Worker;

public sealed record AddAvailablePeriodRequest(DayOfWeek DayOfWeek, string StartTime, string EndTime);
