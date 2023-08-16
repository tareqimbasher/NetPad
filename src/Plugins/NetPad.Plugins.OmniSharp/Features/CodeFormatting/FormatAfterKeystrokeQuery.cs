using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.CodeFormatting;

public class FormatAfterKeystrokeQuery : OmniSharpScriptQuery<OmniSharpFormatAfterKeystrokeRequest, OmniSharpFormatRangeResponse?>
{
    public FormatAfterKeystrokeQuery(Guid scriptId, OmniSharpFormatAfterKeystrokeRequest omniSharpRequest) : base(scriptId, omniSharpRequest)
    {
    }

    public class Handler : IRequestHandler<FormatAfterKeystrokeQuery, OmniSharpFormatRangeResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpFormatRangeResponse?> Handle(FormatAfterKeystrokeQuery request, CancellationToken cancellationToken)
        {
            request.Input.FileName = _server.Project.UserProgramFilePath;

            return await _server.OmniSharpServer.SendAsync<OmniSharpFormatRangeResponse>(request.Input, cancellationToken);
        }
    }
}
