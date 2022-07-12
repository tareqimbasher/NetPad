using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.FindImplementations;

public class FindImplementationsQuery : OmniSharpScriptQuery<OmniSharpFindImplementationsRequest, OmniSharpQuickFixResponse?>
{
    public FindImplementationsQuery(Guid scriptId, OmniSharpFindImplementationsRequest omniSharpRequest) : base(scriptId, omniSharpRequest)
    {
    }

    public class Handler : IRequestHandler<FindImplementationsQuery, OmniSharpQuickFixResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpQuickFixResponse?> Handle(FindImplementationsQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;
            int userCodeStartsOnLine = _server.Project.UserCodeStartsOnLine;

            omniSharpRequest.FileName = _server.Project.ProgramFilePath;
            omniSharpRequest.Line = LineCorrecter.AdjustForOmniSharp(userCodeStartsOnLine, omniSharpRequest.Line);

            var response = await _server.OmniSharpServer.SendAsync<OmniSharpQuickFixResponse>(omniSharpRequest);

            if (response?.QuickFixes == null)
            {
                return response;
            }

            foreach (var quickFix in response.QuickFixes)
            {
                LineCorrecter.AdjustForResponse(userCodeStartsOnLine, quickFix);
            }

            return response;
        }
    }
}
