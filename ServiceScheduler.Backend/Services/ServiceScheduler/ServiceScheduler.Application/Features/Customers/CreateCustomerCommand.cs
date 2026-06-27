using SharedKernel.Abstractions.CQRS;

namespace ServiceScheduler.Application.Features.Customers;

public sealed record CreateCustomerCommand(string Name, string Phone, string Email) : ICommandRequest<Guid>;