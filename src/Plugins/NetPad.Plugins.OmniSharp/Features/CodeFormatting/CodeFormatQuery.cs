using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.CodeFormatting;

public class CodeFormatQuery : OmniSharpScriptQuery<OmniSharpCodeFormatRequest, OmniSharpCodeFormatResponse?>
{
    public CodeFormatQuery(Guid scriptId, OmniSharpCodeFormatRequest omniSharpRequest) : base(scriptId, omniSharpRequest)
    {
    }

    public class Handler : IRequestHandler<CodeFormatQuery, OmniSharpCodeFormatResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpCodeFormatResponse?> Handle(CodeFormatQuery request, CancellationToken cancellationToken)
        {
            return await _server.OmniSharpServer.SendAsync<OmniSharpCodeFormatResponse>(new OmniSharpCodeFormatRequest
            {
                Buffer = request.Input.Buffer,
                FileName = _server.Project.UserProgramFilePath
            }, cancellationToken);
        }
    }
}
