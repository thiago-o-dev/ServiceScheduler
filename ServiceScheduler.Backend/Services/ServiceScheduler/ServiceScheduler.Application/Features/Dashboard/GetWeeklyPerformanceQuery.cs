using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Domain.Entities;
using ServiceScheduler.Domain.Policies;
using SharedKernel.Abstractions.CQRS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceScheduler.Application.Features.Dashboard;

public sealed record GetWeeklyPerformanceQuery(DateTime Date) : IQueryRequest<WeeklyPerformanceDto>;

public sealed class GetWeeklyPerformanceQueryHandler(
    IScheduleRepository scheduleRepository,
    IServiceRepository serviceRepository,
    IServiceBundleRepository bundleRepository
) : IRequestHandler<GetWeeklyPerformanceQuery, WeeklyPerformanceDto>
{
    public async Task<WeeklyPerformanceDto> HandleAsync(GetWeeklyPerformanceQuery query, CancellationToken cancellationToken = default)
    {
        var date = query.Date;
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        var startOfWeek = date.AddDays(-1 * diff).Date;
        var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);

        var schedules = await scheduleRepository.GetByDateRangeAsync(startOfWeek, endOfWeek, cancellationToken);
        var activeSchedules = schedules.Where(s => s.Status != ScheduleStatus.Canceled).ToList();

        var services = await serviceRepository.GetAllAsync(cancellationToken);
        var bundles = await bundleRepository.GetAllAsync(cancellationToken);

        decimal totalRevenue = 0;
        int completedServicesCount = 0;

        // Track services count and revenue
        var serviceStats = new Dictionary<Guid, (string Name, int Count, decimal StandardValue)>();

        foreach (var schedule in activeSchedules)
        {
            var sIds = schedule.Services.Select(x => x.ServiceId).ToList();
            var sObjs = services.Where(x => sIds.Contains(x.Id)).ToList();

            // Calculate NetValue
            decimal netValue;
            if (schedule.CustomNetValue.HasValue)
            {
                netValue = schedule.CustomNetValue.Value;
            }
            else
            {
                netValue = PricingPolicy.CalculateTotalValue(sObjs, bundles.ToList());
            }

            totalRevenue += netValue;

            // Count completed services
            completedServicesCount += schedule.Services.Count(ss => ss.Status == ServiceStatus.Concluded);

            // Group service statistics
            foreach (var ss in schedule.Services)
            {
                var sObj = sObjs.FirstOrDefault(x => x.Id == ss.ServiceId);
                var name = sObj?.Name ?? "Serviço Desconhecido";
                var val = sObj?.Value ?? 0;

                if (!serviceStats.ContainsKey(ss.ServiceId))
                {
                    serviceStats[ss.ServiceId] = (name, 0, val);
                }

                var current = serviceStats[ss.ServiceId];
                serviceStats[ss.ServiceId] = (current.Name, current.Count + 1, current.StandardValue);
            }
        }

        var topServices = serviceStats.Values
            .Select(x => new TopServiceDto(x.Name, x.Count, x.Count * x.StandardValue))
            .OrderByDescending(x => x.Count)
            .ThenByDescending(x => x.Revenue)
            .ToList();

        return new WeeklyPerformanceDto(
            startOfWeek,
            endOfWeek,
            totalRevenue,
            activeSchedules.Count,
            completedServicesCount,
            topServices
        );
    }
}
