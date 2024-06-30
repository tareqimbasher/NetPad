using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.Completion;

public class GetCompletionAfterInsertQuery(Guid scriptId, CompletionItem input)
    : OmniSharpScriptQuery<CompletionItem, OmniSharpCompletionAfterInsertResponse?>(scriptId, input)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<GetCompletionAfterInsertQuery, OmniSharpCompletionAfterInsertResponse?>
    {
        public async Task<OmniSharpCompletionAfterInsertResponse?> Handle(GetCompletionAfterInsertQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            return await server.OmniSharpServer.SendAsync<OmniSharpCompletionAfterInsertResponse>(new OmniSharpCompletionAfterInsertRequest
            {
                Item = omniSharpRequest.ToOmniSharpCompletionItem()
            }, cancellationToken);
        }
    }
}
