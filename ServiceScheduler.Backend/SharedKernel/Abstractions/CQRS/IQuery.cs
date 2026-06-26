namespace SharedKernel.Abstractions.CQRS;

public interface IQuery<out TResponse> : IRequest<TResponse>;