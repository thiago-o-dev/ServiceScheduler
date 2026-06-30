using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Application.Features.Workers;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.Exceptions;

namespace ServiceScheduler.Application.Features.Services;

public sealed record GetServiceByIdQuery(Guid Id) : IQueryRequest<ServiceDto>;

public sealed class GetServiceByIdQueryHandler(IServiceRepository serviceRepository)
    : IRequestHandler<GetServiceByIdQuery, ServiceDto>
{
    public async Task<ServiceDto> HandleAsync(GetServiceByIdQuery query, CancellationToken cancellationToken = default)
    {
        var service = await serviceRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException($"Serviço com ID '{query.Id}' não encontrado.");

        return new ServiceDto(service.Id, service.Name, service.Description, service.Value);
    }
}

public sealed record ListServicesQuery : IQueryRequest<IReadOnlyList<ServiceDto>>;

public sealed class ListServicesQueryHandler(IServiceRepository serviceRepository)
    : IRequestHandler<ListServicesQuery, IReadOnlyList<ServiceDto>>
{
    public async Task<IReadOnlyList<ServiceDto>> HandleAsync(ListServicesQuery query, CancellationToken cancellationToken = default)
    {
        var services = await serviceRepository.GetAllAsync(cancellationToken);

        return services.Select(s => new ServiceDto(s.Id, s.Name, s.Description, s.Value)).ToList();
    }
}

public sealed record GetServiceAvailableHoursQuery(Guid ServiceId, DateTime Start, DateTime End, Guid? WorkerId = null)
    : IQueryRequest<Dictionary<Guid, IReadOnlyList<DateTimeIntervalDto>>>;

public sealed class GetServiceAvailableHoursQueryHandler(
    IServiceRepository serviceRepository,
    IWorkerRepository workerRepository,
    IScheduleRepository scheduleRepository)
    : IRequestHandler<GetServiceAvailableHoursQuery, Dictionary<Guid, IReadOnlyList<DateTimeIntervalDto>>>
{
    public async Task<Dictionary<Guid, IReadOnlyList<DateTimeIntervalDto>>> HandleAsync(
        GetServiceAvailableHoursQuery query,
        CancellationToken cancellationToken = default)
    {
        var service = await serviceRepository.GetByIdAsync(query.ServiceId, cancellationToken)
            ?? throw new NotFoundException($"Serviço com ID '{query.ServiceId}' não encontrado.");

        var workers = query.WorkerId.HasValue ? [await workerRepository.GetByIdAsync((Guid)query.WorkerId, cancellationToken)] : await workerRepository.GetAllAsync(cancellationToken);

        var result = new Dictionary<Guid, IReadOnlyList<DateTimeIntervalDto>>();

        foreach (var worker in workers)
        {
            var schedules = await scheduleRepository.GetByWorkerIdAsync(worker.Id, cancellationToken);
            var availablePeriods = worker.GetAvailablePeriods(query.Start, query.End, schedules);

            if (availablePeriods.Any())
            {
                result[worker.Id] = availablePeriods.Select(p => new DateTimeIntervalDto(p.Start, p.End)).ToList();
            }
        }

        return result;
    }
}

