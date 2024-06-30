using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.SignatureHelp;

public class GetSignatureHelpQuery(Guid scriptId, OmniSharpSignatureHelpRequest input)
    : OmniSharpScriptQuery<OmniSharpSignatureHelpRequest, OmniSharpSignatureHelpResponse?>(scriptId, input)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<GetSignatureHelpQuery, OmniSharpSignatureHelpResponse?>
    {
        public async Task<OmniSharpSignatureHelpResponse?> Handle(GetSignatureHelpQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            omniSharpRequest.FileName = server.Project.UserProgramFilePath;

            return await server.OmniSharpServer.SendAsync<OmniSharpSignatureHelpResponse>(omniSharpRequest, cancellationToken);
        }
    }
}
