using MediatR;
using NetPad.Application;

namespace NetPad.Plugins.OmniSharp.Features.ServerManagement;

public class RestartOmniSharpServerCommand : OmniSharpScriptCommand<bool>
{
    public RestartOmniSharpServerCommand(Guid scriptId) : base(scriptId)
    {
    }

    public class Handler : IRequestHandler<RestartOmniSharpServerCommand, bool>
    {
        private readonly AppOmniSharpServer _server;
        private readonly IAppStatusMessagePublisher _appStatusMessagePublisher;
        private readonly ILogger<Handler> _logger;

        public Handler(AppOmniSharpServer server, IAppStatusMessagePublisher appStatusMessagePublisher, ILogger<Handler> logger)
        {
            _server = server;
            _appStatusMessagePublisher = appStatusMessagePublisher;
            _logger = logger;
        }

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
                success = await _server.RestartAsync(progress => { _appStatusMessagePublisher.PublishAsync(scriptId, progress, persistant: true); });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restart OmniSharp server for script {ScriptId}", scriptId);
                success = false;
            }

            await _appStatusMessagePublisher.PublishAsync(scriptId, $"{(success ? "Restarted" : "Failed to restart")} OmniSharp Server");

            return success;
        }
    }
}
