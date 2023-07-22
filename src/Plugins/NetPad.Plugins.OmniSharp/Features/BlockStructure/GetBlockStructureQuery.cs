using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.BlockStructure;

public class GetBlockStructureQuery : OmniSharpScriptQuery<BlockStructureResponse?>
{
    public GetBlockStructureQuery(Guid scriptId) : base(scriptId)
    {
    }

    public class Handler : IRequestHandler<GetBlockStructureQuery, BlockStructureResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<BlockStructureResponse?> Handle(GetBlockStructureQuery request, CancellationToken cancellationToken)
        {
            return await _server.OmniSharpServer.SendAsync<BlockStructureResponse>(new OmniSharpBlockStructureRequest
            {
                FileName = _server.Project.UserProgramFilePath
            }, cancellationToken);
        }
    }
}
