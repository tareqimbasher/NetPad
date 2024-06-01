using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.SemanticHighlighting;

public class GetSemanticHighlightsQuery(Guid scriptId, OmniSharpSemanticHighlightRequest input)
    : OmniSharpScriptQuery<OmniSharpSemanticHighlightRequest, OmniSharpSemanticHighlightResponse?>(scriptId, input)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<GetSemanticHighlightsQuery, OmniSharpSemanticHighlightResponse?>
    {
        public async Task<OmniSharpSemanticHighlightResponse?> Handle(GetSemanticHighlightsQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            omniSharpRequest.FileName = server.Project.UserProgramFilePath;

            return await server.OmniSharpServer.SendAsync<OmniSharpSemanticHighlightResponse>(omniSharpRequest, cancellationToken);
        }
    }
}
