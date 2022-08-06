using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.SemanticHighlighting;

public class GetSemanticHighlightsQuery : OmniSharpScriptQuery<OmniSharpSemanticHighlightRequest, OmniSharpSemanticHighlightResponse?>
{
    public GetSemanticHighlightsQuery(Guid scriptId, OmniSharpSemanticHighlightRequest input) : base(scriptId, input)
    {
    }

    public class Handler : IRequestHandler<GetSemanticHighlightsQuery, OmniSharpSemanticHighlightResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpSemanticHighlightResponse?> Handle(GetSemanticHighlightsQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            omniSharpRequest.FileName = _server.Project.UserProgramFilePath;

            return await _server.OmniSharpServer.SendAsync<OmniSharpSemanticHighlightResponse>(omniSharpRequest);
        }
    }
}
