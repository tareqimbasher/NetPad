using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.CodeActions;

public class GetCodeActionsQuery : OmniSharpScriptQuery<OmniSharpGetCodeActionsRequest, OmniSharpGetCodeActionsResponse?>
{
    public GetCodeActionsQuery(Guid scriptId, OmniSharpGetCodeActionsRequest input) : base(scriptId, input)
    {
    }

    public class Handler : IRequestHandler<GetCodeActionsQuery, OmniSharpGetCodeActionsResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpGetCodeActionsResponse?> Handle(GetCodeActionsQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;
            int userCodeStartsOnLine = _server.Project.UserCodeStartsOnLine;

            omniSharpRequest.FileName = _server.Project.ProgramFilePath;
            omniSharpRequest.Line = LineCorrecter.AdjustForOmniSharp(userCodeStartsOnLine, omniSharpRequest.Line);

            if (omniSharpRequest.Selection != null)
            {
                omniSharpRequest.Selection = new()
                {
                    Start = LineCorrecter.AdjustForOmniSharp(userCodeStartsOnLine, omniSharpRequest.Selection.Start),
                    End = LineCorrecter.AdjustForOmniSharp(userCodeStartsOnLine, omniSharpRequest.Selection.End)
                };
            }

            return await _server.OmniSharpServer.SendAsync<OmniSharpGetCodeActionsResponse>(omniSharpRequest);
        }
    }
}
