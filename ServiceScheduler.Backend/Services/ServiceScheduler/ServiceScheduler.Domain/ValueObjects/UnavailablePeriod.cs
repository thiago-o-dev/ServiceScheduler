using SharedKernel.Exceptions;

namespace ServiceScheduler.Domain.ValueObjects;

/// <summary>
/// Horarios/Datas de exceção onde não se pode ter agendamentos.
/// </summary>
public sealed class UnavailablePeriod
{
    public DateTime Start { get; }
    public DateTime End { get; }
    public string? Reason { get; }

    public UnavailablePeriod(
        DateTime start,
        DateTime end,
        string? reason = null)
    {
        if (start >= end)
            throw new DomainValidationException("O início deve ser anterior ao fim.");

        Start = start;
        End = end;
        Reason = reason;
    }

    public bool Contains(DateTime dateTime) => dateTime >= Start && dateTime < End;
}
