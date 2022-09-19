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

            omniSharpRequest.FileName = _server.Project.UserProgramFilePath;

            return await _server.OmniSharpServer.SendAsync<OmniSharpSignatureHelpResponse>(omniSharpRequest, cancellationToken);
        }
    }
}
