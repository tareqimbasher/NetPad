using MediatR;
using NetPad.Data;
using NetPad.Data.Metadata;
using NetPad.DotNet;
using NetPad.Sessions;

namespace NetPad.Apps.CQs;

public class RefreshDataConnectionCommand(Guid id) : Command
{
    public Guid ConnectionId { get; } = id;

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
