using MediatR;
using NetPad.Application;

namespace NetPad.Plugins.OmniSharp.Features.ServerManagement;

public class RestartOmniSharpServerCommand(Guid scriptId) : OmniSharpScriptCommand<bool>(scriptId)
{
    public class Handler(AppOmniSharpServer server, IAppStatusMessagePublisher appStatusMessagePublisher, ILogger<Handler> logger)
        : IRequestHandler<RestartOmniSharpServerCommand, bool>
    {
        public async Task<bool> Handle(RestartOmniSharpServerCommand request, CancellationToken cancellationToken)
        {
            var scriptId = request.ScriptId;

            bool success;

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            try
            {
                success = await server.RestartAsync(progress => { appStatusMessagePublisher.PublishAsync(scriptId, progress); });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to restart OmniSharp server for script {ScriptId}", scriptId);
                success = false;
            }

            await appStatusMessagePublisher.PublishAsync(scriptId, $"{(success ? "Restarted" : "Failed to restart")} OmniSharp Server");

            return success;
        }
    }
}
