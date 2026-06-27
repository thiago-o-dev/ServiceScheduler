using SharedKernel.Abstractions.CQRS;

namespace ServiceScheduler.Application.Features.Clients;

public sealed record CreateClientCommand(string Name, string Phone, string Email) : ICommandRequest<Guid>;