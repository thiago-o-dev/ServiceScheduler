using BuildingBlocks.Persistence.Abstractions;
using ServiceScheduler.Application.Abstractions;
using ServiceScheduler.Domain.Entities;
using ServiceScheduler.Domain.Policies;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.Exceptions;

namespace ServiceScheduler.Application.Features.Schedules;

public sealed record CreateScheduleCommand(
    Guid CustomerId,
    Guid WorkerId,
    IReadOnlyList<Guid> ServiceIds,
    DateTime ScheduledAt,
    TimeSpan Duration
) : ICommandRequest<CreateScheduleResultDto>;

public sealed class CreateScheduleCommandHandler(
    ICustomerRepository customerRepository,
    IWorkerRepository workerRepository,
    IServiceRepository serviceRepository,
    IServiceBundleRepository bundleRepository,
    IScheduleRepository scheduleRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateScheduleCommand, CreateScheduleResultDto>
{
    public async Task<CreateScheduleResultDto> HandleAsync(CreateScheduleCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(command.CustomerId, cancellationToken)
            ?? throw new NotFoundException($"Cliente com ID '{command.CustomerId}' não encontrado.");

        var worker = await workerRepository.GetByIdAsync(command.WorkerId, cancellationToken)
            ?? throw new NotFoundException($"Prestador com ID '{command.WorkerId}' não encontrado.");

        var services = await serviceRepository.GetByIdsAsync(command.ServiceIds, cancellationToken);
        if (services.Count != command.ServiceIds.Count)
            throw new NotFoundException("Um ou mais serviços informados não foram encontrados.");

        // Availability Validations
        var schedStart = command.ScheduledAt;
        var schedEnd = command.ScheduledAt + command.Duration;

        // 1. Shift check
        var isAvailable = worker.AvailablePeriods.Any(p =>
            p.DayOfWeek == command.ScheduledAt.DayOfWeek &&
            p.StartTime <= schedStart.TimeOfDay &&
            p.EndTime >= schedEnd.TimeOfDay);
        if (!isAvailable)
            throw new DomainValidationException("O profissional não está disponível neste horário (fora do turno de atendimento).");

        // 2. Unavailable periods check
        var isUnavailable = worker.UnavailablePeriods.Any(up =>
            schedStart < up.End &&
            schedEnd > up.Start);
        if (isUnavailable)
            throw new DomainValidationException("O profissional está indisponível neste período (bloqueio/folga/férias).");

        // 3. Collision check
        var otherSchedules = await scheduleRepository.GetByWorkerIdAsync(worker.Id, cancellationToken);
        var hasCollision = otherSchedules.Any(s =>
            s.Status != ScheduleStatus.Canceled &&
            schedStart < (s.ScheduledAt + s.Duration) &&
            schedEnd > s.ScheduledAt);
        if (hasCollision)
            throw new DomainValidationException("O profissional já possui outro agendamento conflitante neste horário.");

        // Create
        var schedule = Schedule.Create(customer.Id, worker.Id, services.ToList(), command.ScheduledAt, command.Duration);
        await scheduleRepository.AddAsync(schedule, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // NetValue calculation
        var activeBundles = await bundleRepository.GetAllAsync(cancellationToken);
        var netValue = PricingPolicy.CalculateTotalValue(services.ToList(), activeBundles.ToList());

        // Weekly suggestion check
        var weeklySchedules = await scheduleRepository.GetByWeekAsync(customer.Id, command.ScheduledAt, cancellationToken);
        var otherWeeklySchedules = weeklySchedules
            .Where(s => s.Id != schedule.Id)
            .OrderBy(s => s.ScheduledAt)
            .ToList();

        WeeklySuggestionDto weeklySuggestion;
        if (otherWeeklySchedules.Any())
        {
            var firstSchedule = otherWeeklySchedules.First();
            weeklySuggestion = new WeeklySuggestionDto(
                true,
                firstSchedule.ScheduledAt,
                $"Você já possui um agendamento nesta semana para a data {firstSchedule.ScheduledAt:dd/MM/yyyy}. Sugerimos consolidar os serviços no mesmo dia."
            );
        }
        else
        {
            weeklySuggestion = new WeeklySuggestionDto(false, null, null);
        }

        var scheduleDto = new ScheduleDto(
            schedule.Id,
            customer.Id,
            customer.Name,
            worker.Id,
            worker.Name,
            services.Select(s => new ScheduledServiceDto(s.Id, s.Name, s.Value, ServiceStatus.Pending.ToString())).ToList(),
            schedule.ScheduledAt,
            schedule.Duration,
            schedule.BruteValue,
            netValue,
            schedule.Status.ToString()
        );

        return new CreateScheduleResultDto(scheduleDto, weeklySuggestion);
    }
}

public sealed record UpdateScheduleCommand(
    Guid Id,
    Guid WorkerId,
    IReadOnlyList<Guid> ServiceIds,
    DateTime ScheduledAt,
    TimeSpan Duration
) : ICommandRequest;

public sealed class UpdateScheduleCommandHandler(
    IScheduleRepository scheduleRepository,
    IWorkerRepository workerRepository,
    IServiceRepository serviceRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateScheduleCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateScheduleCommand command, CancellationToken cancellationToken = default)
    {
        var schedule = await scheduleRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Agendamento com ID '{command.Id}' não encontrado.");

        // Rule of 2 days
        var minAllowedDate = DateTime.UtcNow.AddDays(2);
        if (schedule.ScheduledAt < minAllowedDate)
            throw new DomainValidationException("Alterações em agendamentos com menos de 2 dias de antecedência devem ser feitas exclusivamente por telefone.");
        if (command.ScheduledAt < minAllowedDate)
            throw new DomainValidationException("O novo horário do agendamento deve ser para pelo menos 2 dias no futuro.");

        var worker = await workerRepository.GetByIdAsync(command.WorkerId, cancellationToken)
            ?? throw new NotFoundException($"Prestador com ID '{command.WorkerId}' não encontrado.");

        var services = await serviceRepository.GetByIdsAsync(command.ServiceIds, cancellationToken);
        if (services.Count != command.ServiceIds.Count)
            throw new NotFoundException("Um ou mais serviços informados não foram encontrados.");

        // Availability check
        var schedStart = command.ScheduledAt;
        var schedEnd = command.ScheduledAt + command.Duration;

        var isAvailable = worker.AvailablePeriods.Any(p =>
            p.DayOfWeek == command.ScheduledAt.DayOfWeek &&
            p.StartTime <= schedStart.TimeOfDay &&
            p.EndTime >= schedEnd.TimeOfDay);
        if (!isAvailable)
            throw new DomainValidationException("O profissional não está disponível neste horário (fora do turno de atendimento).");

        var isUnavailable = worker.UnavailablePeriods.Any(up =>
            schedStart < up.End &&
            schedEnd > up.Start);
        if (isUnavailable)
            throw new DomainValidationException("O profissional está indisponível neste período (bloqueio/folga/férias).");

        var otherSchedules = await scheduleRepository.GetByWorkerIdAsync(worker.Id, cancellationToken);
        var hasCollision = otherSchedules.Any(s =>
            s.Id != schedule.Id &&
            s.Status != ScheduleStatus.Canceled &&
            schedStart < (s.ScheduledAt + s.Duration) &&
            schedEnd > s.ScheduledAt);
        if (hasCollision)
            throw new DomainValidationException("O profissional já possui outro agendamento conflitante neste horário.");

        // Update
        schedule.Update(schedule.CustomerId, worker.Id, command.ScheduledAt, command.Duration);
        schedule.UpdateServices(services.ToList());
        schedule.SetCustomNetValue(null); // Clear override

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public sealed record AdminUpdateScheduleCommand(
    Guid Id,
    Guid CustomerId,
    Guid WorkerId,
    IReadOnlyList<Guid> ServiceIds,
    DateTime ScheduledAt,
    TimeSpan Duration,
    decimal? OverrideNetValue
) : ICommandRequest;

public sealed class AdminUpdateScheduleCommandHandler(
    IScheduleRepository scheduleRepository,
    IWorkerRepository workerRepository,
    IServiceRepository serviceRepository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<AdminUpdateScheduleCommand, Unit>
{
    public async Task<Unit> HandleAsync(AdminUpdateScheduleCommand command, CancellationToken cancellationToken = default)
    {
        var schedule = await scheduleRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Agendamento com ID '{command.Id}' não encontrado.");

        var customer = await customerRepository.GetByIdAsync(command.CustomerId, cancellationToken)
            ?? throw new NotFoundException($"Cliente com ID '{command.CustomerId}' não encontrado.");

        var worker = await workerRepository.GetByIdAsync(command.WorkerId, cancellationToken)
            ?? throw new NotFoundException($"Prestador com ID '{command.WorkerId}' não encontrado.");

        var services = await serviceRepository.GetByIdsAsync(command.ServiceIds, cancellationToken);
        if (services.Count != command.ServiceIds.Count)
            throw new NotFoundException("Um ou mais serviços informados não foram encontrados.");

        // Availability check
        var schedStart = command.ScheduledAt;
        var schedEnd = command.ScheduledAt + command.Duration;

        var isAvailable = worker.AvailablePeriods.Any(p =>
            p.DayOfWeek == command.ScheduledAt.DayOfWeek &&
            p.StartTime <= schedStart.TimeOfDay &&
            p.EndTime >= schedEnd.TimeOfDay);
        if (!isAvailable)
            throw new DomainValidationException("O profissional não está disponível neste horário (fora do turno de atendimento).");

        var isUnavailable = worker.UnavailablePeriods.Any(up =>
            schedStart < up.End &&
            schedEnd > up.Start);
        if (isUnavailable)
            throw new DomainValidationException("O profissional está indisponível neste período (bloqueio/folga/férias).");

        var otherSchedules = await scheduleRepository.GetByWorkerIdAsync(worker.Id, cancellationToken);
        var hasCollision = otherSchedules.Any(s =>
            s.Id != schedule.Id &&
            s.Status != ScheduleStatus.Canceled &&
            schedStart < (s.ScheduledAt + s.Duration) &&
            schedEnd > s.ScheduledAt);
        if (hasCollision)
            throw new DomainValidationException("O profissional já possui outro agendamento conflitante neste horário.");

        // Update
        schedule.Update(customer.Id, worker.Id, command.ScheduledAt, command.Duration);
        schedule.UpdateServices(services.ToList());
        schedule.SetCustomNetValue(command.OverrideNetValue);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public sealed record ConfirmScheduleCommand(Guid Id) : ICommandRequest;

public sealed class ConfirmScheduleCommandHandler(IScheduleRepository scheduleRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmScheduleCommand, Unit>
{
    public async Task<Unit> HandleAsync(ConfirmScheduleCommand command, CancellationToken cancellationToken = default)
    {
        var schedule = await scheduleRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Agendamento com ID '{command.Id}' não encontrado.");

        schedule.Confirm();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public sealed record CancelScheduleCommand(Guid Id) : ICommandRequest;

public sealed class CancelScheduleCommandHandler(IScheduleRepository scheduleRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CancelScheduleCommand, Unit>
{
    public async Task<Unit> HandleAsync(CancelScheduleCommand command, CancellationToken cancellationToken = default)
    {
        var schedule = await scheduleRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Agendamento com ID '{command.Id}' não encontrado.");

        schedule.Cancel();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public sealed record UpdateServiceStatusInScheduleCommand(Guid ScheduleId, Guid ServiceId, string Status) : ICommandRequest;

public sealed class UpdateServiceStatusInScheduleCommandHandler(IScheduleRepository scheduleRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateServiceStatusInScheduleCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateServiceStatusInScheduleCommand command, CancellationToken cancellationToken = default)
    {
        var schedule = await scheduleRepository.GetByIdAsync(command.ScheduleId, cancellationToken)
            ?? throw new NotFoundException($"Agendamento com ID '{command.ScheduleId}' não encontrado.");

        if (!Enum.TryParse<ServiceStatus>(command.Status, true, out var status))
            throw new DomainValidationException($"Status '{command.Status}' é inválido.");

        schedule.UpdateServiceStatus(command.ServiceId, status);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
