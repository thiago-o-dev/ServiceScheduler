namespace ServiceScheduler.Api.Requests.Worker;

public sealed record RemoveAvailablePeriodRequest(DayOfWeek DayOfWeek, TimeSpan StartTime, TimeSpan EndTime);
