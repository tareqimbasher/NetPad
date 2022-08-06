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

            if (omniSharpRequest.Location == null)
            {
                return null;
            }

            omniSharpRequest.Location = new()
            {
                FileName = _server.Project.UserProgramFilePath,
                Range = new()
                {
                    Start = omniSharpRequest.Location.Range.Start,
                    End = omniSharpRequest.Location.Range.End
                }
            };

            return await _server.OmniSharpServer.SendAsync<OmniSharpInlayHintResponse>(omniSharpRequest);
        }
    }
}
