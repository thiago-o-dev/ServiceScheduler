using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Application.Features.Workers;
using ServiceScheduler.Domain.Entities;
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

public sealed record GetServiceAvailableHoursQuery(IReadOnlyList<Guid> ServiceIds, DateTime Start, DateTime End, Guid WorkerId)
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
        if (query.ServiceIds.Count == 0)
            throw new NotFoundException("Informe ao menos um serviço.");

        foreach (var serviceId in query.ServiceIds)
        {
            _ = await serviceRepository.GetByIdAsync(serviceId, cancellationToken)
                ?? throw new NotFoundException($"Serviço com ID '{serviceId}' não encontrado.");
        }

        List<Worker> workers = [];

        if (query.WorkerId != Guid.Empty)
        {
            var worker = await workerRepository.GetByIdAsync(query.WorkerId, cancellationToken)
                ?? throw new NotFoundException("Trabalhador não encontrado");

            workers.Add(worker);
        }
        else
        {
            workers.AddRange(await workerRepository.GetAllAsync(cancellationToken));
        }

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

