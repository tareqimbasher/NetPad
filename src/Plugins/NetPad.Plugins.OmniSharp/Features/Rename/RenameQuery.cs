using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.Rename;


public class RenameQuery(Guid scriptId, OmniSharpRenameRequest omniSharpRequest)
    : OmniSharpScriptQuery<OmniSharpRenameRequest, RenameResponse?>(scriptId, omniSharpRequest)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<RenameQuery, RenameResponse?>
    {
        public async Task<RenameResponse?> Handle(RenameQuery request, CancellationToken cancellationToken)
        {
            request.Input.FileName = server.Project.UserProgramFilePath;

            return await server.OmniSharpServer.SendAsync<RenameResponse>(request.Input, cancellationToken);
        }
    }
}
