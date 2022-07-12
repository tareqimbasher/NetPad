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
            int userCodeStartsOnLine = _server.Project.UserCodeStartsOnLine;

            omniSharpRequest.FileName = _server.Project.ProgramFilePath;

            if (omniSharpRequest.Range != null)
            {
                omniSharpRequest.Range = new()
                {
                    Start = LineCorrecter.AdjustForOmniSharp(userCodeStartsOnLine, omniSharpRequest.Range.Start),
                    End = LineCorrecter.AdjustForOmniSharp(userCodeStartsOnLine, omniSharpRequest.Range.End)
                };
            }

            var response = await _server.OmniSharpServer.SendAsync<OmniSharpSemanticHighlightResponse>(omniSharpRequest);

            if (response != null)
            {
                // Remove any spans before user code
                response.Spans = response.Spans.Where(s => s.StartLine >= userCodeStartsOnLine).ToArray();

                // Adjust line numbers
                foreach (var span in response.Spans)
                {
                    span.StartLine = LineCorrecter.AdjustForResponse(userCodeStartsOnLine, span.StartLine) - 1; // Special case, deduct 1
                    span.EndLine = LineCorrecter.AdjustForResponse(userCodeStartsOnLine, span.EndLine) - 1;
                }
            }

            return response;
        }
    }
}
