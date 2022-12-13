using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.CodeChecking;

public class CheckCodeQuery : OmniSharpScriptQuery<OmniSharpCodeCheckRequest, OmniSharpQuickFixResponse?>
{
    public CheckCodeQuery(Guid scriptId, OmniSharpCodeCheckRequest input) : base(scriptId, input)
    {
    }

    public class Handler : IRequestHandler<CheckCodeQuery, OmniSharpQuickFixResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpQuickFixResponse?> Handle(CheckCodeQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            omniSharpRequest.FileName = _server.Project.UserProgramFilePath;

            return await _server.OmniSharpServer.SendAsync<OmniSharpQuickFixResponse>(omniSharpRequest, cancellationToken);
        }
    }
}
