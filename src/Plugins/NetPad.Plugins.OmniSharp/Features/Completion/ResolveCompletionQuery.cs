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
            var response = await _server.OmniSharpServer.SendAsync<OmniSharpCompletionResolveResponse>(new OmniSharpCompletionResolveRequest
            {
                Item = request.Input.ToOmniSharpCompletionItem()
            });

            if (response?.Item == null)
            {
                return response;
            }

            int userCodeStartsOnLine = _server.Project.UserCodeStartsOnLine;

            // If input.TextEdit isn't null, lines are already adjusted from the GetCompletions query
            if (request.Input.TextEdit == null && response.Item.TextEdit != null)
            {
                response.Item.TextEdit.StartLine = LineCorrecter.AdjustForResponse(userCodeStartsOnLine, response.Item.TextEdit.StartLine);
                response.Item.TextEdit.EndLine = LineCorrecter.AdjustForResponse(userCodeStartsOnLine, response.Item.TextEdit.EndLine);
            }

            // If input.AdditionalTextEdits isn't null, lines are already adjusted from the GetCompletions query
            if (request.Input.AdditionalTextEdits?.Any() != true && response.Item.AdditionalTextEdits?.Any() == true)
            {
                foreach (var additionalTextEdit in response.Item.AdditionalTextEdits)
                {
                    additionalTextEdit.StartLine = LineCorrecter.AdjustForResponse(userCodeStartsOnLine, additionalTextEdit.StartLine);
                    additionalTextEdit.EndLine = LineCorrecter.AdjustForResponse(userCodeStartsOnLine, additionalTextEdit.EndLine);
                }
            }

            return response;
        }
    }
}
