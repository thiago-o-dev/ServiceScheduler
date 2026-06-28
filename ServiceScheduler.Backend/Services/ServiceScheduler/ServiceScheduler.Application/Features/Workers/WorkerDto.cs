namespace ServiceScheduler.Application.Features.Workers;

public record AvailablePeriodDto(DayOfWeek DayOfWeek, string StartTime, string EndTime);

public record UnavailablePeriodDto(DateTime Start, DateTime End, string? Reason);

public record DateTimeIntervalDto(DateTime Start, DateTime End);

public record WorkerDto(
    Guid Id,
    string Name,
    string Phone,
    string Email,
    string Cpf,
    IReadOnlyList<AvailablePeriodDto> AvailablePeriods,
    IReadOnlyList<UnavailablePeriodDto> UnavailablePeriods
);
