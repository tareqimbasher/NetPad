using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.SignatureHelp;

public class GetSignatureHelpQuery : OmniSharpScriptQuery<OmniSharpSignatureHelpRequest, OmniSharpSignatureHelpResponse?>
{
    public GetSignatureHelpQuery(Guid scriptId, OmniSharpSignatureHelpRequest input) : base(scriptId, input)
    {
    }

    public class Handler : IRequestHandler<GetSignatureHelpQuery, OmniSharpSignatureHelpResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpSignatureHelpResponse?> Handle(GetSignatureHelpQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;
            int userCodeStartsOnLine = _server.Project.UserCodeStartsOnLine;

            omniSharpRequest.FileName = _server.Project.ProgramFilePath;
            omniSharpRequest.Line = LineCorrecter.AdjustForOmniSharp(userCodeStartsOnLine, omniSharpRequest.Line);

            return await _server.OmniSharpServer.SendAsync<OmniSharpSignatureHelpResponse>(omniSharpRequest);
        }
    }
}
