using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.FindUsages;

public class FindUsagesQuery(Guid scriptId, OmniSharpFindUsagesRequest input)
    : OmniSharpScriptQuery<OmniSharpFindUsagesRequest, OmniSharpQuickFixResponse?>(scriptId, input)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<FindUsagesQuery, OmniSharpQuickFixResponse?>
    {
        public async Task<OmniSharpQuickFixResponse?> Handle(FindUsagesQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            omniSharpRequest.FileName = server.Project.UserProgramFilePath;

            return await server.OmniSharpServer.SendAsync<OmniSharpQuickFixResponse>(omniSharpRequest, cancellationToken);
        }
    }
}
