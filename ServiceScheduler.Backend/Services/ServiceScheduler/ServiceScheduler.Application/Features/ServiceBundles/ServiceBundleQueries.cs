using ServiceScheduler.Application.Abstractions;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.Exceptions;
using System.Reflection.Metadata;

namespace ServiceScheduler.Application.Features.ServiceBundles;

public sealed record GetServiceBundleByIdQuery(Guid Id) : IQueryRequest<ServiceBundleDto>;

public sealed class GetServiceBundleByIdQueryHandler(IServiceBundleRepository bundleRepository, IServiceRepository serviceRepository)
    : IRequestHandler<GetServiceBundleByIdQuery, ServiceBundleDto>
{
    public async Task<ServiceBundleDto> HandleAsync(GetServiceBundleByIdQuery query, CancellationToken cancellationToken = default)
    {
        var bundle = await bundleRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException($"Pacote com ID '{query.Id}' não encontrado.");

        var services = await serviceRepository.GetAllAsync();

        var servicesGrossValue = services.Where(s => bundle.ServiceIds.Contains(s.Id)).Select(s => s.Value).Sum();

        return new ServiceBundleDto(bundle.Id, bundle.Name, bundle.Description, bundle.ServiceIds.ToList(), bundle.Value, (1 - bundle.Value / servicesGrossValue)*100);
    }
}

public sealed record ListServiceBundlesQuery : IQueryRequest<IReadOnlyList<ServiceBundleDto>>;

public sealed class ListServiceBundlesQueryHandler(IServiceBundleRepository bundleRepository, IServiceRepository serviceRepository)
    : IRequestHandler<ListServiceBundlesQuery, IReadOnlyList<ServiceBundleDto>>
{
    public async Task<IReadOnlyList<ServiceBundleDto>> HandleAsync(ListServiceBundlesQuery query, CancellationToken cancellationToken = default)
    {
        var bundles = await bundleRepository.GetAllAsync(cancellationToken);

        var services = await serviceRepository.GetAllAsync();

        return bundles.Select(b => new ServiceBundleDto(b.Id, b.Name, b.Description, b.ServiceIds.ToList(), b.Value, (1 - b.Value / services.Where(s => b.ServiceIds.Contains(s.Id)).Select(s => s.Value).Sum()) * 100)).ToList();
    }
}
