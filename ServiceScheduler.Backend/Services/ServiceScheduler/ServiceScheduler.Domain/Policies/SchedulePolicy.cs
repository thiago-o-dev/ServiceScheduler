using SharedKernel.Exceptions;

namespace ServiceScheduler.Domain.Policies;

public static class SchedulePolicy
{
    private static readonly TimeSpan MinimumDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaximumDuration = TimeSpan.FromHours(8);

    public static void EnsureAllServiceIdsAreValid(ICollection<Guid> serviceIds)
    {
        if (serviceIds == null)
            throw new DomainValidationException("Serviços não devem ser nulos");

        if (!serviceIds.Any())
            throw new DomainValidationException("Ao menos um serviço deve ser definido.");

        if (serviceIds.Any(id => id == Guid.Empty))
            throw new DomainValidationException("Nenhum serviço pode ser vazio.");
    }

    public static void EnsureScheduledAtIsInFuture(DateTime scheduledAt)
    {
        if (scheduledAt <= DateTime.UtcNow)
            throw new DomainValidationException("O agendamento deve ser realizado para uma data futura.");
    }

    public static void EnsureDurationIsWithinAllowedRange(TimeSpan duration)
    {
        if (duration < MinimumDuration || duration > MaximumDuration)
            throw new DomainValidationException($"A duração do agendamento deve estar entre {MinimumDuration.TotalMinutes:0} minutos e {MaximumDuration.TotalHours:0} horas.");
    }
}
