using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.CodeFormatting;

public class FormatAfterKeystrokeQuery(Guid scriptId, OmniSharpFormatAfterKeystrokeRequest omniSharpRequest)
    : OmniSharpScriptQuery<OmniSharpFormatAfterKeystrokeRequest, OmniSharpFormatRangeResponse?>(scriptId, omniSharpRequest)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<FormatAfterKeystrokeQuery, OmniSharpFormatRangeResponse?>
    {
        public async Task<OmniSharpFormatRangeResponse?> Handle(FormatAfterKeystrokeQuery request, CancellationToken cancellationToken)
        {
            request.Input.FileName = server.Project.UserProgramFilePath;

            return await server.OmniSharpServer.SendAsync<OmniSharpFormatRangeResponse>(request.Input, cancellationToken);
        }
    }
}
