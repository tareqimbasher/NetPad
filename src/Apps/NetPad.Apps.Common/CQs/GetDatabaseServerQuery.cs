using MediatR;
using NetPad.Data;

namespace NetPad.Apps.CQs;

public class GetDatabaseServerQuery(Guid serverId) : Query<DatabaseServerConnection?>
{
    public Guid ServerId { get; } = serverId;

    public class Handler(IDataConnectionRepository dataConnectionRepository)
        : IRequestHandler<GetDatabaseServerQuery, DatabaseServerConnection?>
    {
        public async Task<DatabaseServerConnection?> Handle(GetDatabaseServerQuery request, CancellationToken cancellationToken)
        {
            return await dataConnectionRepository.GetServerAsync(request.ServerId);
        }
    }
}
