using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Domain.Entities;
using ServiceScheduler.Domain.Policies;
using SharedKernel.Abstractions.CQRS;

namespace ServiceScheduler.Application.Features.Dashboard;

public sealed record GetWeeklyPerformanceQuery(DateTime Date) : IQueryRequest<WeeklyPerformanceDto>;

public sealed class GetWeeklyPerformanceQueryHandler(
    IScheduleRepository scheduleRepository,
    IServiceRepository serviceRepository,
    IServiceBundleRepository bundleRepository,
    IWorkerRepository workerRepository
) : IRequestHandler<GetWeeklyPerformanceQuery, WeeklyPerformanceDto>
{
    public async Task<WeeklyPerformanceDto> HandleAsync(GetWeeklyPerformanceQuery query, CancellationToken cancellationToken = default)
    {
        var date = query.Date;

        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        var startOfWeek = date.AddDays(-diff).Date;
        var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);

        var schedules = await scheduleRepository.GetByDateRangeAsync(
            startOfWeek,
            endOfWeek,
            cancellationToken);

        var services = await serviceRepository.GetAllAsync(cancellationToken);
        var bundles = await bundleRepository.GetAllAsync(cancellationToken);
        var workers = await workerRepository.GetAllAsync(cancellationToken);

        // Faster lookups
        var serviceLookup = services.ToDictionary(x => x.Id);
        var workerLookup = workers.ToDictionary(x => x.Id);

        var activeSchedules = schedules
            .Where(s => s.Status != ScheduleStatus.Canceled)
            .ToList();

        decimal totalRevenue = 0m;

        int completed = 0;
        int confirmed = schedules.Count(x => x.Status == ScheduleStatus.Confirmed);
        int cancelled = schedules.Count(x => x.Status == ScheduleStatus.Canceled);

        var serviceStats = new Dictionary<Guid, ServiceAggregate>();
        var workerStats = new Dictionary<Guid, WorkerAggregate>();

        var byDay = new List<WeeklyDayDto>();

        // ----------------------------------------------------
        // Build chart (7 days)
        // ----------------------------------------------------
        for (var i = 0; i < 7; i++)
        {
            var day = startOfWeek.AddDays(i);

            var daySchedules = activeSchedules
                .Where(x => x.ScheduledAt == day.Date)
                .ToList();

            decimal dayRevenue = 0;

            foreach (var schedule in daySchedules)
            {
                var scheduleServices = schedule.Services
                    .Select(x => serviceLookup[x.ServiceId])
                    .ToList();

                decimal scheduleRevenue;

                if (schedule.CustomNetValue.HasValue)
                {
                    scheduleRevenue = schedule.CustomNetValue.Value;
                }
                else
                {
                    scheduleRevenue = PricingPolicy.CalculateTotalValue(
                        scheduleServices,
                        bundles.ToList());
                }

                dayRevenue += scheduleRevenue;
                totalRevenue += scheduleRevenue;

                completed += schedule.Services.Count(x =>
                    x.Status == ServiceStatus.Concluded);

                //
                // Top services
                //
                foreach (var item in schedule.Services)
                {
                    if (!serviceLookup.TryGetValue(item.ServiceId, out var service))
                        continue;

                    if (!serviceStats.TryGetValue(service.Id, out var aggregate))
                    {
                        aggregate = new ServiceAggregate(
                            service.Id,
                            service.Name);

                        serviceStats[service.Id] = aggregate;
                    }

                    aggregate.Count++;
                    aggregate.Revenue += service.Value;
                }

                //
                // Top workers
                //
                if (workerLookup.TryGetValue(schedule.WorkerId, out var worker))
                {
                    if (!workerStats.TryGetValue(worker.Id, out var aggregate))
                    {
                        aggregate = new WorkerAggregate(
                            worker.Id,
                            worker.Name);

                        workerStats[worker.Id] = aggregate;
                    }

                    aggregate.Count++;
                    aggregate.Revenue += scheduleRevenue;
                }
            }

            byDay.Add(new WeeklyDayDto(
                day,
                daySchedules.Count,
                dayRevenue));
        }

        var averageTicket = activeSchedules.Count == 0
            ? 0
            : totalRevenue / activeSchedules.Count;

        var totals = new WeeklyTotalsDto(
            Schedules: activeSchedules.Count,
            Confirmed: confirmed,
            Completed: completed,
            Cancelled: cancelled,
            NetRevenue: totalRevenue,
            AverageTicket: averageTicket);

        var topServices = serviceStats.Values
            .OrderByDescending(x => x.Count)
            .ThenByDescending(x => x.Revenue)
            .Select(x => new TopServiceDto(
                x.Id,
                x.Name,
                x.Count,
                x.Revenue))
            .ToList();

        var topWorkers = workerStats.Values
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.Count)
            .Select(x => new TopWorkerDto(
                x.Id,
                x.Name,
                x.Count,
                x.Revenue))
            .ToList();

        return new WeeklyPerformanceDto(
            totals,
            byDay,
            topServices,
            topWorkers);
    }

    private sealed class ServiceAggregate
    {
        public ServiceAggregate(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public Guid Id { get; }
        public string Name { get; }
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    private sealed class WorkerAggregate
    {
        public WorkerAggregate(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public Guid Id { get; }
        public string Name { get; }
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }
}
