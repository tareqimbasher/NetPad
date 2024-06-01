using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.CodeChecking;

public class CheckCodeQuery(Guid scriptId, OmniSharpCodeCheckRequest input)
    : OmniSharpScriptQuery<OmniSharpCodeCheckRequest, OmniSharpQuickFixResponse?>(scriptId, input)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<CheckCodeQuery, OmniSharpQuickFixResponse?>
    {
        public async Task<OmniSharpQuickFixResponse?> Handle(CheckCodeQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            omniSharpRequest.FileName = server.Project.UserProgramFilePath;

            return await server.OmniSharpServer.SendAsync<OmniSharpQuickFixResponse>(omniSharpRequest, cancellationToken);
        }
    }
}
