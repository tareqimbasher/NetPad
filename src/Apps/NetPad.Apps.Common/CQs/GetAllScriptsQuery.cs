using MediatR;
using NetPad.Scripts;

namespace NetPad.Apps.CQs;

public class GetAllScriptsQuery : Query<IEnumerable<ScriptSummary>>
{
    public class Handler(IScriptRepository scriptRepository)
        : IRequestHandler<GetAllScriptsQuery, IEnumerable<ScriptSummary>>
    {
        public async Task<IEnumerable<ScriptSummary>> Handle(GetAllScriptsQuery request, CancellationToken cancellationToken)
        {
            return await scriptRepository.GetAllAsync();
        }
    }
}
