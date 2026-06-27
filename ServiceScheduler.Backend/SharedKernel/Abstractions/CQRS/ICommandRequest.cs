namespace SharedKernel.Abstractions.CQRS;

public interface ICommandRequest : IRequest<Unit>;

public interface ICommandRequest<out TResponse> : IRequest<TResponse>;