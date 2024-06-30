using MediatR;
using NetPad.Apps.CQs;

namespace NetPad.Services;

public class MediatorRequestPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse?>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    public async Task<TResponse?> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse?> next)
    {
        // All Mediator requests must inherit from QueryBase or CommandBase
        if (!typeof(CommandBase).IsAssignableFrom(typeof(TRequest)) && !typeof(QueryBase).IsAssignableFrom(typeof(TRequest)))
        {
            throw new InvalidOperationException($"{typeof(TRequest)} does not inherit from {nameof(QueryBase)} or from {nameof(CommandBase)}");
        }

        return await next();
    }
}
