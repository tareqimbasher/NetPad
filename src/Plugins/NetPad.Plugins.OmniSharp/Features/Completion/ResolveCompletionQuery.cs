using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.Completion;

public class ResolveCompletionQuery : OmniSharpScriptQuery<CompletionItem, OmniSharpCompletionResolveResponse?>
{
    public ResolveCompletionQuery(Guid scriptId, CompletionItem completionItem) : base(scriptId, completionItem)
    {
    }

    public class Handler : IRequestHandler<ResolveCompletionQuery, OmniSharpCompletionResolveResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpCompletionResolveResponse?> Handle(ResolveCompletionQuery request, CancellationToken cancellationToken)
        {
            return await _server.OmniSharpServer.SendAsync<OmniSharpCompletionResolveResponse>(new OmniSharpCompletionResolveRequest
            {
                Item = request.Input.ToOmniSharpCompletionItem()
            }, cancellationToken);
        }
    }
}
