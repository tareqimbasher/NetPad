using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.FindImplementations;

public class FindImplementationsQuery(Guid scriptId, OmniSharpFindImplementationsRequest omniSharpRequest)
    : OmniSharpScriptQuery<OmniSharpFindImplementationsRequest, OmniSharpQuickFixResponse?>(scriptId, omniSharpRequest)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<FindImplementationsQuery, OmniSharpQuickFixResponse?>
    {
        public async Task<OmniSharpQuickFixResponse?> Handle(FindImplementationsQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            omniSharpRequest.FileName = server.Project.UserProgramFilePath;

            return await server.OmniSharpServer.SendAsync<OmniSharpQuickFixResponse>(omniSharpRequest, cancellationToken);
        }
    }
}
