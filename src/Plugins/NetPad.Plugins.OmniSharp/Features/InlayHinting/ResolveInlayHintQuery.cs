using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.InlayHinting;

public class ResolveInlayHintQuery(Guid scriptId, InlayHintResolveRequest input)
    : OmniSharpScriptQuery<InlayHintResolveRequest, OmniSharpInlayHint?>(scriptId, input)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<ResolveInlayHintQuery, OmniSharpInlayHint?>
    {
        public async Task<OmniSharpInlayHint?> Handle(ResolveInlayHintQuery request, CancellationToken cancellationToken)
        {
            var input = request.Input;

            return await server.OmniSharpServer.SendAsync<OmniSharpInlayHint>(input.ToOmniSharpInlayHintResolveRequest(), cancellationToken);
        }
    }
}
