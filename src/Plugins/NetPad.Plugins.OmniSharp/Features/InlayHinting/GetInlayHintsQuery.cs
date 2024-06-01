using MediatR;
using OmniSharp.Models.V2;

namespace NetPad.Plugins.OmniSharp.Features.InlayHinting;

public class GetInlayHintsQuery(Guid scriptId, OmniSharpInlayHintRequest input)
    : OmniSharpScriptQuery<OmniSharpInlayHintRequest, OmniSharpInlayHintResponse?>(scriptId, input)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<GetInlayHintsQuery, OmniSharpInlayHintResponse?>
    {
        public async Task<OmniSharpInlayHintResponse?> Handle(GetInlayHintsQuery request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            if (omniSharpRequest.Location == null)
            {
                return null;
            }

            omniSharpRequest.Location = new Location
            {
                FileName = server.Project.UserProgramFilePath,
                Range = new OmniSharpRange
                {
                    Start = omniSharpRequest.Location.Range.Start,
                    End = omniSharpRequest.Location.Range.End
                }
            };

            return await server.OmniSharpServer.SendAsync<OmniSharpInlayHintResponse>(omniSharpRequest, cancellationToken);
        }
    }
}
