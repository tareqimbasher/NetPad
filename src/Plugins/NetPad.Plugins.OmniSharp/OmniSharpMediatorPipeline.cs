using MediatR;
using NetPad.Plugins.OmniSharp.Exceptions;
using NetPad.Plugins.OmniSharp.Features;

namespace NetPad.Plugins.OmniSharp;

public class OmniSharpMediatorPipeline<TRequest, TResponse>(IServiceProvider serviceProvider, ILogger<OmniSharpMediatorPipeline<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse?>
    where TRequest : class, IRequest<TResponse>
{
    public async Task<TResponse?> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse?> next)
    {
        // This pipeline should only process requests from current assembly
        if (typeof(OmniSharpPlugin).Assembly != typeof(TRequest).Assembly)
        {
            return await next();
        }

        if (request is ITargetSpecificOmniSharpServer targetsSpecificOmniSharp)
        {
            var serverCatalog = serviceProvider.GetRequiredService<OmniSharpServerCatalog>();

            var server = await serverCatalog.GetOmniSharpServerAsync(targetsSpecificOmniSharp.ScriptId);
            if (server == null)
            {
                bool isResponseNullable = Nullable.GetUnderlyingType(typeof(TResponse)) != null;
                if (isResponseNullable)
                {
                    return default;
                }

                throw new OmniSharpInstanceNotFound(targetsSpecificOmniSharp.ScriptId);
            }

            serviceProvider.GetRequiredService<AppOmniSharpServerAccessor>().Set(server);
        }

        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred processing request of type '{RequestType}' with an expected response of type '{ResponseType}'",
                typeof(TRequest),
                typeof(TResponse));
            throw;
        }
    }
}
