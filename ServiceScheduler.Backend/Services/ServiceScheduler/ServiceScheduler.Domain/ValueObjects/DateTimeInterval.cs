using SharedKernel.Exceptions;

namespace ServiceScheduler.Domain.ValueObjects;

public record DateTimeInterval
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public DateTimeInterval(DateTime start, DateTime end)
    {
        if (start >= end)
            throw new DomainValidationException("O início do período deve ser anterior ao fim.");

        Start = start;
        End = end;
    }
}
