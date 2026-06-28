namespace ServiceScheduler.Api.Requests.Worker;

public sealed record RemoveAvailablePeriodRequest(DayOfWeek DayOfWeek, string StartTime, string EndTime);
