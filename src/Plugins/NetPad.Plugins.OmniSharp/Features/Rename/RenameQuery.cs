using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.Rename;


public class RenameQuery : OmniSharpScriptQuery<OmniSharpRenameRequest, RenameResponse?>
{
    public RenameQuery(Guid scriptId, OmniSharpRenameRequest omniSharpRequest) : base(scriptId, omniSharpRequest)
    {
    }

    public class Handler : IRequestHandler<RenameQuery, RenameResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<RenameResponse?> Handle(RenameQuery request, CancellationToken cancellationToken)
        {
            request.Input.FileName = _server.Project.UserProgramFilePath;

            return await _server.OmniSharpServer.SendAsync<RenameResponse>(request.Input, cancellationToken);
        }
    }
}
