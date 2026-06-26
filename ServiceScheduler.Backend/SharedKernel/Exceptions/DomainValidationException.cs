namespace SharedKernel.Exceptions;

public class DomainValidationException(string message) : Exception(message);
