using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.Completion;

public class ResolveCompletionQuery(Guid scriptId, CompletionItem completionItem)
    : OmniSharpScriptQuery<CompletionItem, OmniSharpCompletionResolveResponse?>(scriptId, completionItem)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<ResolveCompletionQuery, OmniSharpCompletionResolveResponse?>
    {
        public async Task<OmniSharpCompletionResolveResponse?> Handle(ResolveCompletionQuery request, CancellationToken cancellationToken)
        {
            return await server.OmniSharpServer.SendAsync<OmniSharpCompletionResolveResponse>(new OmniSharpCompletionResolveRequest
            {
                Item = request.Input.ToOmniSharpCompletionItem()
            }, cancellationToken);
        }
    }
}
