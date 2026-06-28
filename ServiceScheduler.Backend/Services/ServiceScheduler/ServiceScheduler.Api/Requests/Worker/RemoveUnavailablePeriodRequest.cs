namespace ServiceScheduler.Api.Requests.Worker;

public sealed record RemoveUnavailablePeriodRequest(DateTime Start, DateTime End);
