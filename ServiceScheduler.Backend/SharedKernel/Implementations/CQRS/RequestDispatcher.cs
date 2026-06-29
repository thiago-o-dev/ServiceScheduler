using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.CQRS;

namespace SharedKernel.Implementations.CQRS;

internal sealed class RequestDispatcher(IServiceProvider serviceProvider)
    : IRequestDispatcher
{
    public Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var handlerType = typeof(IRequestHandler<,>)
            .MakeGenericType(request.GetType(), typeof(TResponse));

        dynamic handler = serviceProvider.GetRequiredService(handlerType);

        return handler.HandleAsync((dynamic)request, cancellationToken);
    }
}
