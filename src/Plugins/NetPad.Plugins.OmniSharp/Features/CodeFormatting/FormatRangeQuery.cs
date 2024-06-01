using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.CodeFormatting;

public class FormatRangeQuery(Guid scriptId, OmniSharpFormatRangeRequest omniSharpRequest)
    : OmniSharpScriptQuery<OmniSharpFormatRangeRequest, OmniSharpFormatRangeResponse?>(scriptId, omniSharpRequest)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<FormatRangeQuery, OmniSharpFormatRangeResponse?>
    {
        public async Task<OmniSharpFormatRangeResponse?> Handle(FormatRangeQuery request, CancellationToken cancellationToken)
        {
            request.Input.FileName = server.Project.UserProgramFilePath;

            return await server.OmniSharpServer.SendAsync<OmniSharpFormatRangeResponse>(request.Input, cancellationToken);
        }
    }
}
