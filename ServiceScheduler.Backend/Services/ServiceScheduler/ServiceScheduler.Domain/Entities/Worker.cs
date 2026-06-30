using ServiceScheduler.Domain.Policies;
using ServiceScheduler.Domain.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Exceptions;

namespace ServiceScheduler.Domain.Entities;

public class Worker : LifeCycleEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Cpf { get; private set; } = string.Empty;

    public ICollection<AvailablePeriod> AvailablePeriods { get; private set; } = new List<AvailablePeriod>();
    public ICollection<UnavailablePeriod> UnavailablePeriods { get; private set; } = new List<UnavailablePeriod>();

    private Worker() { }

    public static Worker Create(
        string name,
        string phone,
        string email,
        string cpf,
        ICollection<AvailablePeriod>? availablePeriods = null,
        ICollection<UnavailablePeriod>? unavailablePeriods = null)
    {
        WorkerPolicy.EnsureNameIsValid(name);
        WorkerPolicy.EnsurePhoneIsValid(phone);
        WorkerPolicy.EnsureEmailIsValid(email);
        WorkerPolicy.EnsureCpfIsValid(cpf);

        availablePeriods ??= new List<AvailablePeriod>();
        unavailablePeriods ??= new List<UnavailablePeriod>();

        WorkerPolicy.EnsureAvailablePeriodsDoNotOverlap(availablePeriods);
        WorkerPolicy.EnsureUnavailablePeriodsDoNotOverlap(unavailablePeriods);

        return new Worker
        {
            Name = name.Trim(),
            Phone = phone.Trim(),
            Email = email.Trim(),
            Cpf = cpf.Trim(),
            AvailablePeriods = availablePeriods,
            UnavailablePeriods = unavailablePeriods
        };
    }

    public void Update(
        string name,
        string phone,
        string email,
        string cpf)
    {
        WorkerPolicy.EnsureNameIsValid(name);
        WorkerPolicy.EnsurePhoneIsValid(phone);
        WorkerPolicy.EnsureEmailIsValid(email);
        WorkerPolicy.EnsureCpfIsValid(cpf);

        Name = name.Trim();
        Phone = phone.Trim();
        Email = email.Trim();
        Cpf = cpf.Trim();

        Touch();
    }

    public void AddAvailablePeriod(AvailablePeriod period)
    {
        WorkerPolicy.EnsureAvailablePeriodCanBeAdded(
            period,
            AvailablePeriods);

        AvailablePeriods.Add(period);

        Touch();
    }

    public void RemoveAvailablePeriod(AvailablePeriod period)
    {
        if (!AvailablePeriods.Remove(period))
            throw new DomainValidationException("Período de disponibilidade não encontrado.");

        Touch();
    }

    public void AddUnavailablePeriod(UnavailablePeriod period)
    {
        WorkerPolicy.EnsureUnavailablePeriodCanBeAdded(
            period,
            UnavailablePeriods);

        UnavailablePeriods.Add(period);

        Touch();
    }

    public void RemoveUnavailablePeriod(UnavailablePeriod period)
    {
        if (!UnavailablePeriods.Remove(period))
            throw new DomainValidationException("Período de indisponibilidade não encontrado.");

        Touch();
    }

    public void PreemptivelyEndCurrentUnavailablePeriod(DateTime endDate)
    {
        var current = UnavailablePeriods.FirstOrDefault(x => x.Start <= DateTime.UtcNow && x.End > DateTime.UtcNow);

        if (current is null)
            throw new DomainValidationException("Não existe um período de indisponibilidade em andamento.");

        if (endDate <= current.Start)
            throw new DomainValidationException("A nova data de término deve ser posterior ao início da indisponibilidade.");

        UnavailablePeriods.Remove(current);

        UnavailablePeriods.Add(
            new UnavailablePeriod(
                current.Start,
                endDate,
                current.Reason));

        Touch();
    }

    public IEnumerable<DateTimeInterval> GetAvailablePeriods(DateTime start, DateTime end, IEnumerable<Schedule> workerSchedules)
    {
        if (start >= end)
            throw new DomainValidationException("A data de início deve ser anterior à data de término.");

        var availableIntervals = new List<DateTimeInterval>();

        // 1. Generate candidate intervals from AvailablePeriods for each day in range
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            var dayPeriods = AvailablePeriods.Where(p => p.DayOfWeek == date.DayOfWeek);
            foreach (var period in dayPeriods)
            {
                var periodStart = date.Add(period.StartTime);
                var periodEnd = date.Add(period.EndTime);

                // Clip to query range [start, end]
                var actualStart = periodStart > start ? periodStart : start;
                var actualEnd = periodEnd < end ? periodEnd : end;

                if (actualStart < actualEnd)
                {
                    availableIntervals.Add(new DateTimeInterval(actualStart, actualEnd));
                }
            }
        }

        // 2. Collect all blockers (UnavailablePeriods + active Schedules)
        var blockers = new List<DateTimeInterval>();

        // Add unavailable periods that overlap with [start, end]
        foreach (var up in UnavailablePeriods)
        {
            if (up.Start < end && up.End > start)
            {
                var blockStart = up.Start > start ? up.Start : start;
                var blockEnd = up.End < end ? up.End : end;
                if (blockStart < blockEnd)
                {
                    blockers.Add(new DateTimeInterval(blockStart, blockEnd));
                }
            }
        }

        // Add active schedules that overlap with [start, end]
        foreach (var schedule in workerSchedules)
        {
            if (schedule.Status != ScheduleStatus.Cancelled)
            {
                var schedStart = schedule.ScheduledAt;
                var schedEnd = schedule.ScheduledAt + schedule.Duration;

                if (schedStart < end && schedEnd > start)
                {
                    var blockStart = schedStart > start ? schedStart : start;
                    var blockEnd = schedEnd < end ? schedEnd : end;
                    if (blockStart < blockEnd)
                    {
                        blockers.Add(new DateTimeInterval(blockStart, blockEnd));
                    }
                }
            }
        }

        // Sort blockers by start time to make subtraction clean
        var sortedBlockers = blockers.OrderBy(b => b.Start).ToList();

        // 3. Subtract blockers from available intervals
        var result = new List<DateTimeInterval>(availableIntervals);

        foreach (var blocker in sortedBlockers)
        {
            var nextResult = new List<DateTimeInterval>();
            foreach (var av in result)
            {
                if (blocker.End <= av.Start || blocker.Start >= av.End)
                {
                    // No overlap
                    nextResult.Add(av);
                }
                else
                {
                    // Overlap
                    if (blocker.Start > av.Start)
                    {
                        nextResult.Add(new DateTimeInterval(av.Start, blocker.Start));
                    }
                    if (blocker.End < av.End)
                    {
                        nextResult.Add(new DateTimeInterval(blocker.End, av.End));
                    }
                }
            }
            result = nextResult;
        }

        return result.OrderBy(r => r.Start).ToList();
    }
}
