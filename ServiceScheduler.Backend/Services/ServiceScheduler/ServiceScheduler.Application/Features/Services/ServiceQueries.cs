using ServiceScheduler.Application.Abstractions;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
