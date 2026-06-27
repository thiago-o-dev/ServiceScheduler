using BuildingBlocks.Persistence.Abstractions;
using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Application.Exceptions;
using ServiceScheduler.Domain.Entities;
using ServiceScheduler.Domain.ValueObjects;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceScheduler.Application.Features.Workers;

public sealed record CreateWorkerCommand(string Name, string Phone, string Email, string Cpf) : ICommandRequest<Guid>;

public sealed class CreateWorkerCommandHandler(IWorkerRepository workerRepository, IUnitOfWork unitOfWork) 
    : IRequestHandler<CreateWorkerCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateWorkerCommand command, CancellationToken cancellationToken = default)
    {
        if (await workerRepository.ExistsByEmailAsync(command.Email, cancellationToken))
            throw new DuplicateEntityException($"Um prestador com o email '{command.Email}' já existe.");

        var worker = Worker.Create(command.Name, command.Phone, command.Email, command.Cpf);

        await workerRepository.AddAsync(worker, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return worker.Id;
    }
}

public sealed record UpdateWorkerCommand(Guid Id, string Name, string Phone, string Email, string Cpf) : ICommandRequest;

public sealed class UpdateWorkerCommandHandler(IWorkerRepository workerRepository, IUnitOfWork unitOfWork) 
    : IRequestHandler<UpdateWorkerCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateWorkerCommand command, CancellationToken cancellationToken = default)
    {
        var worker = await workerRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Prestador com ID '{command.Id}' não encontrado.");

        worker.Update(command.Name, command.Phone, command.Email, command.Cpf);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public sealed record AddAvailablePeriodCommand(Guid WorkerId, DayOfWeek DayOfWeek, string StartTime, string EndTime) : ICommandRequest;

public sealed class AddAvailablePeriodCommandHandler(IWorkerRepository workerRepository, IUnitOfWork unitOfWork) 
    : IRequestHandler<AddAvailablePeriodCommand, Unit>
{
    public async Task<Unit> HandleAsync(AddAvailablePeriodCommand command, CancellationToken cancellationToken = default)
    {
        var worker = await workerRepository.GetByIdAsync(command.WorkerId, cancellationToken)
            ?? throw new NotFoundException($"Prestador com ID '{command.WorkerId}' não encontrado.");

        var start = TimeSpan.Parse(command.StartTime);
        var end = TimeSpan.Parse(command.EndTime);
        var period = new AvailablePeriod(command.DayOfWeek, start, end);

        worker.AddAvailablePeriod(period);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public sealed record RemoveAvailablePeriodCommand(Guid WorkerId, DayOfWeek DayOfWeek, string StartTime, string EndTime) : ICommandRequest;

public sealed class RemoveAvailablePeriodCommandHandler(IWorkerRepository workerRepository, IUnitOfWork unitOfWork) 
    : IRequestHandler<RemoveAvailablePeriodCommand, Unit>
{
    public async Task<Unit> HandleAsync(RemoveAvailablePeriodCommand command, CancellationToken cancellationToken = default)
    {
        var worker = await workerRepository.GetByIdAsync(command.WorkerId, cancellationToken)
            ?? throw new NotFoundException($"Prestador com ID '{command.WorkerId}' não encontrado.");

        var start = TimeSpan.Parse(command.StartTime);
        var end = TimeSpan.Parse(command.EndTime);
        var period = new AvailablePeriod(command.DayOfWeek, start, end);

        worker.RemoveAvailablePeriod(period);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public sealed record AddUnavailablePeriodCommand(Guid WorkerId, DateTime Start, DateTime End, string? Reason) : ICommandRequest;

public sealed class AddUnavailablePeriodCommandHandler(IWorkerRepository workerRepository, IUnitOfWork unitOfWork) 
    : IRequestHandler<AddUnavailablePeriodCommand, Unit>
{
    public async Task<Unit> HandleAsync(AddUnavailablePeriodCommand command, CancellationToken cancellationToken = default)
    {
        var worker = await workerRepository.GetByIdAsync(command.WorkerId, cancellationToken)
            ?? throw new NotFoundException($"Prestador com ID '{command.WorkerId}' não encontrado.");

        var period = new UnavailablePeriod(command.Start, command.End, command.Reason);

        worker.AddUnavailablePeriod(period);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public sealed record RemoveUnavailablePeriodCommand(Guid WorkerId, DateTime Start, DateTime End) : ICommandRequest;

public sealed class RemoveUnavailablePeriodCommandHandler(IWorkerRepository workerRepository, IUnitOfWork unitOfWork) 
    : IRequestHandler<RemoveUnavailablePeriodCommand, Unit>
{
    public async Task<Unit> HandleAsync(RemoveUnavailablePeriodCommand command, CancellationToken cancellationToken = default)
    {
        var worker = await workerRepository.GetByIdAsync(command.WorkerId, cancellationToken)
            ?? throw new NotFoundException($"Prestador com ID '{command.WorkerId}' não encontrado.");

        var period = new UnavailablePeriod(command.Start, command.End);

        worker.RemoveUnavailablePeriod(period);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public sealed record PreemptUnavailablePeriodCommand(Guid WorkerId, DateTime EndDate) : ICommandRequest;

public sealed class PreemptUnavailablePeriodCommandHandler(IWorkerRepository workerRepository, IUnitOfWork unitOfWork) 
    : IRequestHandler<PreemptUnavailablePeriodCommand, Unit>
{
    public async Task<Unit> HandleAsync(PreemptUnavailablePeriodCommand command, CancellationToken cancellationToken = default)
    {
        var worker = await workerRepository.GetByIdAsync(command.WorkerId, cancellationToken)
            ?? throw new NotFoundException($"Prestador com ID '{command.WorkerId}' não encontrado.");

        worker.PreemptivelyEndCurrentUnavailablePeriod(command.EndDate);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
