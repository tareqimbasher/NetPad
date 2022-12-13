using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.FindImplementations;

public class FindImplementationsQuery : OmniSharpScriptQuery<OmniSharpFindImplementationsRequest, OmniSharpQuickFixResponse?>
{
    public FindImplementationsQuery(Guid scriptId, OmniSharpFindImplementationsRequest omniSharpRequest) : base(scriptId, omniSharpRequest)
    {
    }

    public class Handler : IRequestHandler<FindImplementationsQuery, OmniSharpQuickFixResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpQuickFixResponse?> Handle(FindImplementationsQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            omniSharpRequest.FileName = _server.Project.UserProgramFilePath;

            return await _server.OmniSharpServer.SendAsync<OmniSharpQuickFixResponse>(omniSharpRequest, cancellationToken);
        }
    }
}
