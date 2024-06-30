using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.CodeStructure;

public class GetCodeStructureQuery(Guid scriptId) : OmniSharpScriptQuery<CodeStructureResponse?>(scriptId)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<GetCodeStructureQuery, CodeStructureResponse?>
    {
        public async Task<CodeStructureResponse?> Handle(GetCodeStructureQuery request, CancellationToken cancellationToken)
        {
            return await server.OmniSharpServer.SendAsync<CodeStructureResponse>(new OmniSharpCodeStructureRequest
            {
                FileName = server.Project.UserProgramFilePath
            }, cancellationToken);
        }
    }
}
