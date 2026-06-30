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
    public bool Equals(AvailablePeriod? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return DayOfWeek == other.DayOfWeek
            && StartTime == other.StartTime
            && EndTime == other.EndTime;
    }

    public override bool Equals(object? obj) => Equals(obj as AvailablePeriod);

    public override int GetHashCode() => HashCode.Combine(DayOfWeek, StartTime, EndTime);

    public static bool operator ==(AvailablePeriod? left, AvailablePeriod? right) =>
        Equals(left, right);

    public static bool operator !=(AvailablePeriod? left, AvailablePeriod? right) =>
        !Equals(left, right);
}
