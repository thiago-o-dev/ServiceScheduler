using SharedKernel.Exceptions;

namespace ServiceScheduler.Domain.Exceptions;

public class DomainOverlaidException(string message) : DomainValidationException(message);
