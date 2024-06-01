using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.Diagnostics;

public class StartDiagnosticsCommand(Guid scriptId) : OmniSharpScriptCommand(scriptId)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<StartDiagnosticsCommand>
    {
        public async Task<Unit> Handle(StartDiagnosticsCommand request, CancellationToken cancellationToken)
        {
            await server.OmniSharpServer.SendAsync(new OmniSharpDiagnosticRequest
            {
                FileName = server.Project.UserProgramFilePath
            }, cancellationToken);

            return Unit.Value;
        }
    }
}
