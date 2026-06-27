using BuildingBlocks.Persistence.Abstractions;
using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Application.Exceptions;
using ServiceScheduler.Domain.Entities;
using SharedKernel.Abstractions.CQRS;

namespace ServiceScheduler.Application.Features.Customers;

public sealed class CreateCustomerCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork) : IRequestHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        if (await customerRepository.ExistsByEmailAsync(command.Email, cancellationToken))
            throw new DuplicateEntityException($"Um customere com o email '{command.Email}' já existe.");

        var owner = Customer.Create(command.Name, command.Phone, command.Email);

        await customerRepository.AddAsync(owner, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return owner.Id;
    }
}
