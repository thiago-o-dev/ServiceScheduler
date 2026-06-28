using BuildingBlocks.Persistence.Abstractions;
using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Domain.Entities;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.Exceptions;

namespace ServiceScheduler.Application.Features.Services;

public sealed record CreateServiceCommand(string Name, string Description, decimal Value) : ICommandRequest<Guid>;

public sealed class CreateServiceCommandHandler(IServiceRepository serviceRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateServiceCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateServiceCommand command, CancellationToken cancellationToken = default)
    {
        var service = Service.Create(command.Name, command.Description, command.Value);

        await serviceRepository.AddAsync(service, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return service.Id;
    }
}

public sealed record UpdateServiceCommand(Guid Id, string Name, string Description, decimal Value) : ICommandRequest;

public sealed class UpdateServiceCommandHandler(IServiceRepository serviceRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateServiceCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateServiceCommand command, CancellationToken cancellationToken = default)
    {
        var service = await serviceRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Serviço com ID '{command.Id}' não encontrado.");

        service.Update(command.Name, command.Description, command.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
