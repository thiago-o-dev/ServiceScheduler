using ServiceScheduler.Application.Abstractions;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceScheduler.Application.Features.Workers;

public sealed record GetWorkerByIdQuery(Guid Id) : IQueryRequest<WorkerDto>;

public sealed class GetWorkerByIdQueryHandler(IWorkerRepository workerRepository) 
    : IRequestHandler<GetWorkerByIdQuery, WorkerDto>
{
    public async Task<WorkerDto> HandleAsync(GetWorkerByIdQuery query, CancellationToken cancellationToken = default)
    {
        var worker = await workerRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException($"Prestador com ID '{query.Id}' não encontrado.");

        return MapToDto(worker);
    }

    private static WorkerDto MapToDto(Domain.Entities.Worker worker)
    {
        return new WorkerDto(
            worker.Id,
            worker.Name,
            worker.Phone,
            worker.Email,
            worker.Cpf,
            worker.AvailablePeriods.Select(ap => new AvailablePeriodDto(ap.DayOfWeek, ap.StartTime.ToString(@"hh\:mm"), ap.EndTime.ToString(@"hh\:mm"))).ToList(),
            worker.UnavailablePeriods.Select(up => new UnavailablePeriodDto(up.Start, up.End, up.Reason)).ToList()
        );
    }
}

public sealed record ListWorkersQuery : IQueryRequest<IReadOnlyList<WorkerDto>>;

public sealed class ListWorkersQueryHandler(IWorkerRepository workerRepository) 
    : IRequestHandler<ListWorkersQuery, IReadOnlyList<WorkerDto>>
{
    public async Task<IReadOnlyList<WorkerDto>> HandleAsync(ListWorkersQuery query, CancellationToken cancellationToken = default)
    {
        var workers = await workerRepository.GetAllAsync(cancellationToken);

        return workers.Select(w => new WorkerDto(
            w.Id,
            w.Name,
            w.Phone,
            w.Email,
            w.Cpf,
            w.AvailablePeriods.Select(ap => new AvailablePeriodDto(ap.DayOfWeek, ap.StartTime.ToString(@"hh\:mm"), ap.EndTime.ToString(@"hh\:mm"))).ToList(),
            w.UnavailablePeriods.Select(up => new UnavailablePeriodDto(up.Start, up.End, up.Reason)).ToList()
        )).ToList();
    }
}

public sealed record GetWorkerAvailablePeriodsQuery(Guid WorkerId, DateTime Start, DateTime End) : IQueryRequest<IReadOnlyList<DateTimeIntervalDto>>;

public sealed class GetWorkerAvailablePeriodsQueryHandler(
    IWorkerRepository workerRepository,
    IScheduleRepository scheduleRepository) 
    : IRequestHandler<GetWorkerAvailablePeriodsQuery, IReadOnlyList<DateTimeIntervalDto>>
{
    public async Task<IReadOnlyList<DateTimeIntervalDto>> HandleAsync(GetWorkerAvailablePeriodsQuery query, CancellationToken cancellationToken = default)
    {
        var worker = await workerRepository.GetByIdAsync(query.WorkerId, cancellationToken)
            ?? throw new NotFoundException($"Prestador com ID '{query.WorkerId}' não encontrado.");

        var schedules = await scheduleRepository.GetByWorkerIdAsync(worker.Id, cancellationToken);

        var availablePeriods = worker.GetAvailablePeriods(query.Start, query.End, schedules);

        return availablePeriods.Select(p => new DateTimeIntervalDto(p.Start, p.End)).ToList();
    }
}

