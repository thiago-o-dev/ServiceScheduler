namespace ServiceScheduler.Api.Requests.Worker;

public sealed record AddUnavailablePeriodRequest(DateTime Start, DateTime End, string? Reason);
