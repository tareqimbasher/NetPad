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
            int userCodeStartsOnLine = _server.Project.UserCodeStartsOnLine;

            omniSharpRequest.Line = LineCorrecter.AdjustForOmniSharp(userCodeStartsOnLine, omniSharpRequest.Line) + 1; // Special case, add 1
            omniSharpRequest.FileName = _server.Project.ProgramFilePath;

            return await _server.OmniSharpServer.SendAsync<OmniSharpCompletionResponse>(omniSharpRequest);
        }
    }
}
