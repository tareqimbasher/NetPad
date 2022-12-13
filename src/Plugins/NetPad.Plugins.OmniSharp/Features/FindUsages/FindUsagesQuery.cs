using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.FindUsages;

public class FindUsagesQuery : OmniSharpScriptQuery<OmniSharpFindUsagesRequest, OmniSharpQuickFixResponse?>
{
    public FindUsagesQuery(Guid scriptId, OmniSharpFindUsagesRequest input) : base(scriptId, input)
    {
    }

    public class Handler : IRequestHandler<FindUsagesQuery, OmniSharpQuickFixResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpQuickFixResponse?> Handle(FindUsagesQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            omniSharpRequest.FileName = _server.Project.UserProgramFilePath;

            return await _server.OmniSharpServer.SendAsync<OmniSharpQuickFixResponse>(omniSharpRequest, cancellationToken);
        }
    }
}
