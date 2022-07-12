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

        public Handler(AppOmniSharpServer server, IAppStatusMessagePublisher appStatusMessagePublisher)
        {
            _server = server;
            _appStatusMessagePublisher = appStatusMessagePublisher;
        }

        public async Task<bool> Handle(RestartOmniSharpServerCommand request, CancellationToken cancellationToken)
        {
            var scriptId = request.ScriptId;

            var result = await _server.RestartAsync((progress) => { _appStatusMessagePublisher.PublishAsync(scriptId, progress); });

            await _appStatusMessagePublisher.PublishAsync(scriptId, $"{(result ? "Restarted" : "Failed to restart")} OmniSharp Server");

            return result;
        }
    }
}
