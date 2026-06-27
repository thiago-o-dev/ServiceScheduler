namespace SharedKernel.Abstractions.CQRS;

public interface IQueryRequest<out TResponse> : IRequest<TResponse>;