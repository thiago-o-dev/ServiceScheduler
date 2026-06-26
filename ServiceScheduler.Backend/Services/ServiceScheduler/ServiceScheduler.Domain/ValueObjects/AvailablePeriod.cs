using SharedKernel.Exceptions;

namespace ServiceScheduler.Domain.ValueObjects;

/// <summary>
/// Horario semanal (da pra fazer ele funcionar em dia do mes tambem mas semanal fica melhor) recorrente
/// </summary>
public class AvailablePeriod
{
    public DayOfWeek DayOfWeek { get; }
    public TimeSpan StartTime { get; }
    public TimeSpan EndTime { get; }

    public AvailablePeriod(
        DayOfWeek dayOfWeek,
        TimeSpan startTime,
        TimeSpan endTime)
    {
        if (startTime >= endTime)
            throw new DomainValidationException("Horario de inicio deve ser antes do fim do turno.");

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
    }
}
