using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.Diagnostics;

public class StartDiagnosticsCommand : OmniSharpScriptCommand
{
    public StartDiagnosticsCommand(Guid scriptId) : base(scriptId)
    {
    }

    public class Handler : IRequestHandler<StartDiagnosticsCommand>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<Unit> Handle(StartDiagnosticsCommand request, CancellationToken cancellationToken)
        {
            await _server.OmniSharpServer.SendAsync(new OmniSharpDiagnosticRequest
            {
                FileName = _server.Project.UserProgramFilePath
            });

            return Unit.Value;
        }
    }
}
