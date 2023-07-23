using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.CodeFormatting;

public class FormatRangeQuery : OmniSharpScriptQuery<OmniSharpFormatRangeRequest, OmniSharpFormatRangeResponse?>
{
    public FormatRangeQuery(Guid scriptId, OmniSharpFormatRangeRequest omniSharpRequest) : base(scriptId, omniSharpRequest)
    {
    }

    public class Handler : IRequestHandler<FormatRangeQuery, OmniSharpFormatRangeResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<OmniSharpFormatRangeResponse?> Handle(FormatRangeQuery request, CancellationToken cancellationToken)
        {
            request.Input.FileName = _server.Project.UserProgramFilePath;

            return await _server.OmniSharpServer.SendAsync<OmniSharpFormatRangeResponse>(request.Input, cancellationToken);
        }
    }
}
