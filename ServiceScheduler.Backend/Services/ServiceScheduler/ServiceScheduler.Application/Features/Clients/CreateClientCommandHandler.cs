using BuildingBlocks.Persistence.Abstractions;
using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Application.Exceptions;
using ServiceScheduler.Domain.Entities;
using SharedKernel.Abstractions.CQRS;

namespace ServiceScheduler.Application.Features.Clients;

public sealed class CreateClientCommandHandler(IClientRepository clientRepository, IUnitOfWork unitOfWork) : IRequestHandler<CreateClientCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateClientCommand command, CancellationToken cancellationToken = default)
    {
        if (await clientRepository.GetByEmailAsync(command.Email, cancellationToken) is not null)
            throw new DuplicateEntityException($"Um cliente com o email '{command.Email}' já existe.");

        var owner = Client.Create(command.Name, command.Phone, command.Email);

        await clientRepository.AddAsync(owner, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return owner.Id;
    }
}
