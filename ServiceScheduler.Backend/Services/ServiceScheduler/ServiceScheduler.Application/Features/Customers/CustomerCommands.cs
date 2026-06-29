using BuildingBlocks.Persistence.Abstractions;
using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Application.Exceptions;
using ServiceScheduler.Domain.Entities;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.Exceptions;

namespace ServiceScheduler.Application.Features.Customers;

public sealed record CreateCustomerCommand(string Name, string Phone, string Email) : ICommandRequest<Guid>;

public sealed class CreateCustomerCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork) : IRequestHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        if (await customerRepository.ExistsByEmailAsync(command.Email, cancellationToken))
            throw new DuplicateEntityException($"Um customer com o email '{command.Email}' já existe.");

        var owner = Customer.Create(command.Name, command.Phone, command.Email);

        await customerRepository.AddAsync(owner, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return owner.Id;
    }
}

public sealed record UpdateCustomerCommand(Guid Id, string Name, string Phone) : ICommandRequest;

public sealed class UpdateCustomerCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCustomerCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Cliente com ID '{command.Id}' não encontrado.");

        customer.Update(command.Name, command.Phone);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public sealed record DeleteCustomerCommand(Guid Id) : ICommandRequest;

public sealed class DeleteCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IScheduleRepository scheduleRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCustomerCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Cliente com ID '{command.Id}' não encontrado.");

        // Cascade delete customer schedules synchronously
        var schedules = await scheduleRepository.GetByCustomerIdAsync(customer.Id, cancellationToken);
        foreach (var schedule in schedules)
        {
            scheduleRepository.Remove(schedule);
        }

        customerRepository.Remove(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
