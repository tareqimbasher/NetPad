using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.CodeStructure;

public class GetCodeStructureQuery : OmniSharpScriptQuery<CodeStructureResponse?>
{
    public GetCodeStructureQuery(Guid scriptId) : base(scriptId)
    {
    }

    public class Handler : IRequestHandler<GetCodeStructureQuery, CodeStructureResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<CodeStructureResponse?> Handle(GetCodeStructureQuery request, CancellationToken cancellationToken)
        {
            return await _server.OmniSharpServer.SendAsync<CodeStructureResponse>(new OmniSharpCodeStructureRequest
            {
                FileName = _server.Project.UserProgramFilePath
            });
        }
    }
}
