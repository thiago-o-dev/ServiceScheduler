using BuildingBlocks.Persistence.Abstractions;
using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Domain.Entities;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceScheduler.Application.Features.ServiceBundles;

public sealed record CreateServiceBundleCommand(string Name, string Description, IReadOnlyList<Guid> ServiceIds, decimal Value) : ICommandRequest<Guid>;

public sealed class CreateServiceBundleCommandHandler(IServiceBundleRepository bundleRepository, IUnitOfWork unitOfWork) 
    : IRequestHandler<CreateServiceBundleCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateServiceBundleCommand command, CancellationToken cancellationToken = default)
    {
        var bundle = ServiceBundle.Create(command.Name, command.Description, command.ServiceIds.ToList(), command.Value);

        await bundleRepository.AddAsync(bundle, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return bundle.Id;
    }
}

public sealed record UpdateServiceBundleCommand(Guid Id, string Name, string Description, IReadOnlyList<Guid> ServiceIds, decimal Value) : ICommandRequest;

public sealed class UpdateServiceBundleCommandHandler(IServiceBundleRepository bundleRepository, IUnitOfWork unitOfWork) 
    : IRequestHandler<UpdateServiceBundleCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateServiceBundleCommand command, CancellationToken cancellationToken = default)
    {
        var bundle = await bundleRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Pacote com ID '{command.Id}' não encontrado.");

        bundle.Update(command.Name, command.Description, command.ServiceIds.ToList(), command.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
