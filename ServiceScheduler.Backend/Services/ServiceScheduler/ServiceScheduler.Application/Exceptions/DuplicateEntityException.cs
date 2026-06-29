using SharedKernel.Exceptions;

namespace ServiceScheduler.Application.Exceptions;

public class DuplicateEntityException(string message) : ConflictException(message);
