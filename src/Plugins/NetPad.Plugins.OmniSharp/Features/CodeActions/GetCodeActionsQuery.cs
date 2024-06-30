using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.CodeActions;

public class GetCodeActionsQuery(Guid scriptId, OmniSharpGetCodeActionsRequest input)
    : OmniSharpScriptQuery<OmniSharpGetCodeActionsRequest, OmniSharpGetCodeActionsResponse?>(scriptId, input)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<GetCodeActionsQuery, OmniSharpGetCodeActionsResponse?>
    {
        public async Task<OmniSharpGetCodeActionsResponse?> Handle(GetCodeActionsQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            omniSharpRequest.FileName = server.Project.UserProgramFilePath;

            return await server.OmniSharpServer.SendAsync<OmniSharpGetCodeActionsResponse>(omniSharpRequest, cancellationToken);
        }
    }
}
