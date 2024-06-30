using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.QuickInfo;

public class GetQuickInfoQuery(Guid scriptId, OmniSharpQuickInfoRequest input)
    : OmniSharpScriptQuery<OmniSharpQuickInfoRequest, OmniSharpQuickInfoResponse?>(scriptId, input)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<GetQuickInfoQuery, OmniSharpQuickInfoResponse?>
    {
        public async Task<OmniSharpQuickInfoResponse?> Handle(GetQuickInfoQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            omniSharpRequest.FileName = server.Project.UserProgramFilePath;

            return await server.OmniSharpServer.SendAsync<OmniSharpQuickInfoResponse>(omniSharpRequest, cancellationToken);
        }
    }
}
