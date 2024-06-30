using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.Completion;

public class GetCompletionsQuery(Guid scriptId, OmniSharpCompletionRequest omniSharpRequest)
    : OmniSharpScriptQuery<OmniSharpCompletionRequest, OmniSharpCompletionResponse?>(scriptId, omniSharpRequest)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<GetCompletionsQuery, OmniSharpCompletionResponse?>
    {
        public async Task<OmniSharpCompletionResponse?> Handle(GetCompletionsQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            omniSharpRequest.FileName = server.Project.UserProgramFilePath;

            return await server.OmniSharpServer.SendAsync<OmniSharpCompletionResponse>(omniSharpRequest, cancellationToken);
        }
    }
}
