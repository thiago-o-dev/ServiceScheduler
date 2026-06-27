using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Domain.Entities;
using ServiceScheduler.Domain.Policies;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceScheduler.Application.Features.Schedules;

public sealed record GetScheduleByIdQuery(Guid Id) : IQueryRequest<ScheduleDto>;

public sealed class GetScheduleByIdQueryHandler(
    IScheduleRepository scheduleRepository,
    ICustomerRepository customerRepository,
    IWorkerRepository workerRepository,
    IServiceRepository serviceRepository,
    IServiceBundleRepository bundleRepository
) : IRequestHandler<GetScheduleByIdQuery, ScheduleDto>
{
    public async Task<ScheduleDto> HandleAsync(GetScheduleByIdQuery query, CancellationToken cancellationToken = default)
    {
        var schedule = await scheduleRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException($"Agendamento com ID '{query.Id}' não encontrado.");

        var customer = await customerRepository.GetByIdAsync(schedule.CustomerId, cancellationToken)
            ?? throw new NotFoundException($"Cliente com ID '{schedule.CustomerId}' não encontrado.");

        var worker = await workerRepository.GetByIdAsync(schedule.WorkerId, cancellationToken)
            ?? throw new NotFoundException($"Prestador com ID '{schedule.WorkerId}' não encontrado.");

        var serviceIds = schedule.Services.Select(s => s.ServiceId).ToList();
        var services = await serviceRepository.GetByIdsAsync(serviceIds, cancellationToken);

        decimal netValue;
        if (schedule.CustomNetValue.HasValue)
        {
            netValue = schedule.CustomNetValue.Value;
        }
        else
        {
            var bundles = await bundleRepository.GetAllAsync(cancellationToken);
            netValue = PricingPolicy.CalculateTotalValue(services.ToList(), bundles.ToList());
        }

        var serviceDtos = schedule.Services.Select(ss =>
        {
            var sObj = services.FirstOrDefault(x => x.Id == ss.ServiceId);
            return new ScheduledServiceDto(
                ss.ServiceId,
                sObj?.Name ?? "Serviço Desconhecido",
                sObj?.Value ?? 0,
                ss.Status.ToString()
            );
        }).ToList();

        return new ScheduleDto(
            schedule.Id,
            customer.Id,
            customer.Name,
            worker.Id,
            worker.Name,
            serviceDtos,
            schedule.ScheduledAt,
            schedule.Duration,
            schedule.BruteValue,
            netValue,
            schedule.Status.ToString()
        );
    }
}

public sealed record ListSchedulesQuery(
    Guid? CustomerId,
    Guid? WorkerId,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Status
) : IQueryRequest<IReadOnlyList<ScheduleDto>>;

public sealed class ListSchedulesQueryHandler(
    IScheduleRepository scheduleRepository,
    ICustomerRepository customerRepository,
    IWorkerRepository workerRepository,
    IServiceRepository serviceRepository,
    IServiceBundleRepository bundleRepository
) : IRequestHandler<ListSchedulesQuery, IReadOnlyList<ScheduleDto>>
{
    public async Task<IReadOnlyList<ScheduleDto>> HandleAsync(ListSchedulesQuery query, CancellationToken cancellationToken = default)
    {
        var schedules = await scheduleRepository.GetAllAsync(cancellationToken);

        // Filters
        var filtered = schedules.AsEnumerable();

        if (query.CustomerId.HasValue)
            filtered = filtered.Where(s => s.CustomerId == query.CustomerId.Value);

        if (query.WorkerId.HasValue)
            filtered = filtered.Where(s => s.WorkerId == query.WorkerId.Value);

        if (query.StartDate.HasValue)
            filtered = filtered.Where(s => s.ScheduledAt >= query.StartDate.Value);

        if (query.EndDate.HasValue)
            filtered = filtered.Where(s => s.ScheduledAt <= query.EndDate.Value);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (Enum.TryParse<ScheduleStatus>(query.Status, true, out var status))
            {
                filtered = filtered.Where(s => s.Status == status);
            }
        }

        var result = filtered.ToList();
        if (!result.Any())
            return Array.Empty<ScheduleDto>();

        // Load referenced entities to map DTOs
        var customers = await customerRepository.GetAllAsync(cancellationToken);
        var workers = await workerRepository.GetAllAsync(cancellationToken);
        var services = await serviceRepository.GetAllAsync(cancellationToken);
        var bundles = await bundleRepository.GetAllAsync(cancellationToken);

        var list = new List<ScheduleDto>();
        foreach (var s in result)
        {
            var custName = customers.FirstOrDefault(c => c.Id == s.CustomerId)?.Name ?? "Cliente Desconhecido";
            var workName = workers.FirstOrDefault(w => w.Id == s.WorkerId)?.Name ?? "Prestador Desconhecido";

            var sIds = s.Services.Select(x => x.ServiceId).ToList();
            var sObjs = services.Where(x => sIds.Contains(x.Id)).ToList();

            decimal netValue;
            if (s.CustomNetValue.HasValue)
            {
                netValue = s.CustomNetValue.Value;
            }
            else
            {
                netValue = PricingPolicy.CalculateTotalValue(sObjs, bundles.ToList());
            }

            var serviceDtos = s.Services.Select(ss =>
            {
                var sObj = sObjs.FirstOrDefault(x => x.Id == ss.ServiceId);
                return new ScheduledServiceDto(
                    ss.ServiceId,
                    sObj?.Name ?? "Serviço Desconhecido",
                    sObj?.Value ?? 0,
                    ss.Status.ToString()
                );
            }).ToList();

            list.Add(new ScheduleDto(
                s.Id,
                s.CustomerId,
                custName,
                s.WorkerId,
                workName,
                serviceDtos,
                s.ScheduledAt,
                s.Duration,
                s.BruteValue,
                netValue,
                s.Status.ToString()
            ));
        }

        return list;
    }
}
