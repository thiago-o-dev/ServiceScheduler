using System;
using System.Collections.Generic;

namespace ServiceScheduler.Application.Features.Dashboard;

public record TopServiceDto(string ServiceName, int Count, decimal Revenue);

public record WeeklyPerformanceDto(
    DateTime WeekStartDate,
    DateTime WeekEndDate,
    decimal TotalRevenue,
    int TotalSchedules,
    int CompletedServicesCount,
    IReadOnlyList<TopServiceDto> TopServices
);
