namespace ServiceScheduler.Application.Features.Dashboard;

public record WeeklyTotalsDto(
    int Schedules,
    int Confirmed,
    int Completed,
    int Cancelled,
    decimal NetRevenue,
    decimal AverageTicket);

public record WeeklyDayDto(
    DateTime Date,
    int Schedules,
    decimal Revenue);

public record TopServiceDto(
    Guid ServiceId,
    string ServiceName,
    int Count,
    decimal Revenue);

public record TopWorkerDto(
    Guid WorkerId,
    string WorkerName,
    int Count,
    decimal Revenue);

public record WeeklyPerformanceDto(
    WeeklyTotalsDto Totals,
    IReadOnlyList<WeeklyDayDto> ByDay,
    IReadOnlyList<TopServiceDto> TopServices,
    IReadOnlyList<TopWorkerDto> TopWorkers);