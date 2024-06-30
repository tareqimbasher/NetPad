using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.BlockStructure;

public class GetBlockStructureQuery(Guid scriptId) : OmniSharpScriptQuery<BlockStructureResponse?>(scriptId)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<GetBlockStructureQuery, BlockStructureResponse?>
    {
        public async Task<BlockStructureResponse?> Handle(GetBlockStructureQuery request, CancellationToken cancellationToken)
        {
            return await server.OmniSharpServer.SendAsync<BlockStructureResponse>(new OmniSharpBlockStructureRequest
            {
                FileName = server.Project.UserProgramFilePath
            }, cancellationToken);
        }
    }
}
