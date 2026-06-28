namespace ServiceScheduler.Application.Features.Schedules;

public record WeeklySuggestionDto(bool HasSuggestion, DateTime? SuggestedDate, string? Message);

public record ScheduledServiceDto(Guid ServiceId, string Name, decimal Value, string Status);

public record ScheduleDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid WorkerId,
    string WorkerName,
    IReadOnlyList<ScheduledServiceDto> Services,
    DateTime ScheduledAt,
    TimeSpan Duration,
    decimal BruteValue,
    decimal NetValue,
    string Status
);

public record CreateScheduleResultDto(
    ScheduleDto Schedule,
    WeeklySuggestionDto WeeklySuggestion
);
