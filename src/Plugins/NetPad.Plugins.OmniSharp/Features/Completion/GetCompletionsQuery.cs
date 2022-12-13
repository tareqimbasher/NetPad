using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.Completion;

public class GetCompletionsQuery : OmniSharpScriptQuery<OmniSharpCompletionRequest, OmniSharpCompletionResponse?>
{
    public GetCompletionsQuery(Guid scriptId, OmniSharpCompletionRequest omniSharpRequest) : base(scriptId, omniSharpRequest)
    {
    }

    public class Handler : IRequestHandler<GetCompletionsQuery, OmniSharpCompletionResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpCompletionResponse?> Handle(GetCompletionsQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            omniSharpRequest.FileName = _server.Project.UserProgramFilePath;

            return await _server.OmniSharpServer.SendAsync<OmniSharpCompletionResponse>(omniSharpRequest, cancellationToken);
        }
    }
}
