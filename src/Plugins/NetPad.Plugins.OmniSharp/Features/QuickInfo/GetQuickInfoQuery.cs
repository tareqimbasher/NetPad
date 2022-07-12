using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.QuickInfo;

public class GetQuickInfoQuery : OmniSharpScriptQuery<OmniSharpQuickInfoRequest, OmniSharpQuickInfoResponse?>
{
    public GetQuickInfoQuery(Guid scriptId, OmniSharpQuickInfoRequest input) : base(scriptId, input)
    {
    }

    public class Handler : IRequestHandler<GetQuickInfoQuery, OmniSharpQuickInfoResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpQuickInfoResponse?> Handle(GetQuickInfoQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;
            int userCodeStartsOnLine = _server.Project.UserCodeStartsOnLine;

            omniSharpRequest.FileName = _server.Project.ProgramFilePath;
            omniSharpRequest.Line = LineCorrecter.AdjustForOmniSharp(userCodeStartsOnLine, omniSharpRequest.Line);

            return await _server.OmniSharpServer.SendAsync<OmniSharpQuickInfoResponse>(omniSharpRequest);
        }
    }
}
