using MediatR;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.Apps.CQs;

public class GetScriptsInfoQuery(string? nameFilter = null) : Query<IList<ScriptInfo>>
{
    public string? NameFilter { get; } = nameFilter;

    public class Handler(ISession session, IScriptRepository scriptRepository)
        : IRequestHandler<GetScriptsInfoQuery, IList<ScriptInfo>>
    {
        public async Task<IList<ScriptInfo>> Handle(GetScriptsInfoQuery request,
            CancellationToken cancellationToken)
        {
            var environments = session.GetOpened();
            var summaries = await scriptRepository.GetSummariesAsync();
            var openScriptIds = new HashSet<Guid>(environments.Select(e => e.Script.Id));

            var openScriptInfos = environments.Select(e => new ScriptInfo(
                e.Script.Id,
                e.Script.Name,
                e.Script.Path,
                e.Script.Config.Kind,
                e.Script.Config.TargetFrameworkVersion,
                e.Script.DataConnection?.Id,
                IsOpen: true,
                e.Script.IsDirty,
                e.Status,
                e.RunDurationMilliseconds
            )).ToList();

            var savedScriptInfos = summaries
                .Where(s => !openScriptIds.Contains(s.Id))
                .Select(s => new ScriptInfo(
                    s.Id,
                    s.Name,
                    s.Path,
                    s.Kind,
                    s.TargetFrameworkVersion,
                    s.DataConnectionId,
                    IsOpen: false,
                    IsDirty: false,
                    Status: null,
                    RunDurationMilliseconds: null
                ));

            openScriptInfos.AddRange(savedScriptInfos);

            if (!string.IsNullOrWhiteSpace(request.NameFilter))
            {
                return openScriptInfos
                    .Where(s => s.Name.Contains(request.NameFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return openScriptInfos;
        }
    }
}
