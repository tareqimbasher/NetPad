using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.Completion;

public class GetCompletionAfterInsertQuery : OmniSharpScriptQuery<CompletionItem, OmniSharpCompletionAfterInsertResponse?>
{
    public GetCompletionAfterInsertQuery(Guid scriptId, CompletionItem input) : base(scriptId, input)
    {
    }

    public class Handler : IRequestHandler<GetCompletionAfterInsertQuery, OmniSharpCompletionAfterInsertResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpCompletionAfterInsertResponse?> Handle(GetCompletionAfterInsertQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            return await _server.OmniSharpServer.SendAsync<OmniSharpCompletionAfterInsertResponse>(new OmniSharpCompletionAfterInsertRequest
            {
                Item = omniSharpRequest.ToOmniSharpCompletionItem()
            }, cancellationToken);
        }
    }
}
