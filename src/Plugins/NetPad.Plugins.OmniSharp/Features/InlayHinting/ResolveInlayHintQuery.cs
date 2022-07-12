using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.InlayHinting;

public class ResolveInlayHintQuery : OmniSharpScriptQuery<InlayHintResolveRequest, OmniSharpInlayHint?>
{
    public ResolveInlayHintQuery(Guid scriptId, InlayHintResolveRequest input) : base(scriptId, input)
    {
    }

    public class Handler : IRequestHandler<ResolveInlayHintQuery, OmniSharpInlayHint?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpInlayHint?> Handle(ResolveInlayHintQuery request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            int userCodeStartsOnLine = _server.Project.UserCodeStartsOnLine;

            input.Hint.Position = LineCorrecter.AdjustForOmniSharp(userCodeStartsOnLine, input.Hint.Position);

            var response = await _server.OmniSharpServer.SendAsync<OmniSharpInlayHint>(input.ToOmniSharpInlayHintResolveRequest());

            if (response == null)
            {
                return response;
            }

            response.Position = LineCorrecter.AdjustForResponse(userCodeStartsOnLine, response.Position);

            return response;
        }
    }
}
