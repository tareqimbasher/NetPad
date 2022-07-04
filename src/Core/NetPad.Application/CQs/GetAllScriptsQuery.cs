using MediatR;
using NetPad.Scripts;

namespace NetPad.CQs;

public class GetAllScriptsQuery : Query<IEnumerable<ScriptSummary>>
{
    public class Handler : IRequestHandler<GetAllScriptsQuery, IEnumerable<ScriptSummary>>
    {
        private readonly IScriptRepository _scriptRepository;

        public Handler(IScriptRepository scriptRepository)
        {
            _scriptRepository = scriptRepository;
        }

        public async Task<IEnumerable<ScriptSummary>> Handle(GetAllScriptsQuery request, CancellationToken cancellationToken)
        {
            return await _scriptRepository.GetAllAsync();
        }
    }
}
