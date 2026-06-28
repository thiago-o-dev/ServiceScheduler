using ServiceScheduler.Application.Abstractions;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.Exceptions;

namespace ServiceScheduler.Application.Features.ServiceBundles;

public sealed record GetServiceBundleByIdQuery(Guid Id) : IQueryRequest<ServiceBundleDto>;

public sealed class GetServiceBundleByIdQueryHandler(IServiceBundleRepository bundleRepository)
    : IRequestHandler<GetServiceBundleByIdQuery, ServiceBundleDto>
{
    public async Task<ServiceBundleDto> HandleAsync(GetServiceBundleByIdQuery query, CancellationToken cancellationToken = default)
    {
        var bundle = await bundleRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException($"Pacote com ID '{query.Id}' não encontrado.");

        return new ServiceBundleDto(bundle.Id, bundle.Name, bundle.Description, bundle.ServiceIds.ToList(), bundle.Value);
    }
}

public sealed record ListServiceBundlesQuery : IQueryRequest<IReadOnlyList<ServiceBundleDto>>;

public sealed class ListServiceBundlesQueryHandler(IServiceBundleRepository bundleRepository)
    : IRequestHandler<ListServiceBundlesQuery, IReadOnlyList<ServiceBundleDto>>
{
    public async Task<IReadOnlyList<ServiceBundleDto>> HandleAsync(ListServiceBundlesQuery query, CancellationToken cancellationToken = default)
    {
        var bundles = await bundleRepository.GetAllAsync(cancellationToken);

        return bundles.Select(b => new ServiceBundleDto(b.Id, b.Name, b.Description, b.ServiceIds.ToList(), b.Value)).ToList();
    }
}
