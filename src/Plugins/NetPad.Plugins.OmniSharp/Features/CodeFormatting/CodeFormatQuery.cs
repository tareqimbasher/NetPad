using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.CodeFormatting;

[Obsolete($"Use {nameof(FormatRangeQuery)} and {nameof(FormatAfterKeystrokeQuery)} instead")]
public class CodeFormatQuery(Guid scriptId, OmniSharpCodeFormatRequest omniSharpRequest)
    : OmniSharpScriptQuery<OmniSharpCodeFormatRequest, OmniSharpCodeFormatResponse?>(scriptId, omniSharpRequest)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<CodeFormatQuery, OmniSharpCodeFormatResponse?>
    {
        public async Task<OmniSharpCodeFormatResponse?> Handle(CodeFormatQuery request, CancellationToken cancellationToken)
        {
            return await server.OmniSharpServer.SendAsync<OmniSharpCodeFormatResponse>(new OmniSharpCodeFormatRequest
            {
                Buffer = request.Input.Buffer,
                FileName = server.Project.UserProgramFilePath
            }, cancellationToken);
        }
    }
}
