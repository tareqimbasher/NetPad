using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.InlayHinting;

public class GetInlayHintsQuery : OmniSharpScriptQuery<OmniSharpInlayHintRequest, OmniSharpInlayHintResponse?>
{
    public GetInlayHintsQuery(Guid scriptId, OmniSharpInlayHintRequest input) : base(scriptId, input)
    {
    }

    public class Handler : IRequestHandler<GetInlayHintsQuery, OmniSharpInlayHintResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpInlayHintResponse?> Handle(GetInlayHintsQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;
            int userCodeStartsOnLine = _server.Project.UserCodeStartsOnLine;

            if (omniSharpRequest.Location == null)
            {
                return null;
            }

            omniSharpRequest.Location = new()
            {
                FileName = _server.Project.ProgramFilePath,
                Range = new()
                {
                    Start = LineCorrecter.AdjustForOmniSharp(userCodeStartsOnLine, omniSharpRequest.Location.Range.Start),
                    End = LineCorrecter.AdjustForOmniSharp(userCodeStartsOnLine, omniSharpRequest.Location.Range.End)
                }
            };

            var response = await _server.OmniSharpServer.SendAsync<OmniSharpInlayHintResponse>(omniSharpRequest);

            if (response == null)
            {
                return response;
            }

            foreach (var inlayHint in response.InlayHints)
            {
                inlayHint.Position = LineCorrecter.AdjustForResponse(userCodeStartsOnLine, inlayHint.Position);
            }

            return response;
        }
    }
}
