using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Data;
using NetPad.Data.Metadata;
using NetPad.DotNet;
using NetPad.Sessions;

namespace NetPad.Apps.CQs;

public class RefreshDataConnectionCommand(Guid id) : Command
{
    public Guid ConnectionId { get; } = id;

    public static void RunInBackground(Guid connectionId, IServiceScopeFactory serviceScopeFactory, ILogger logger)
    {
        _ = Task.Run(async () =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            try
            {
                await mediator.Send(new RefreshDataConnectionCommand(connectionId));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing resources for connection {ConnectionId}", connectionId);
            }
        });
    }

    public class Handler(
        IDataConnectionResourcesCache dataConnectionResourcesCache,
        IDataConnectionRepository dataConnectionRepository,
        ISession session,
        IDotNetInfo dotNetInfo)
        : IRequestHandler<RefreshDataConnectionCommand, Unit>
    {
        public async Task<Unit> Handle(RefreshDataConnectionCommand request, CancellationToken cancellationToken)
        {
            var currentActiveScriptTargetFrameworkVersion = session.Active?.Script.Config.TargetFrameworkVersion;

            var connection = await dataConnectionRepository.GetAsync(request.ConnectionId);

            if (connection == null)
            {
                return Unit.Value;
            }

            await dataConnectionResourcesCache.RemoveCachedResourcesAsync(request.ConnectionId);

            var targetFramework = currentActiveScriptTargetFrameworkVersion
                                  ?? dotNetInfo.GetLatestSupportedDotNetSdkVersionOrThrow().GetFrameworkVersion();

            await dataConnectionResourcesCache.GetResourcesAsync(connection, targetFramework);

            return Unit.Value;
        }
    }
}
