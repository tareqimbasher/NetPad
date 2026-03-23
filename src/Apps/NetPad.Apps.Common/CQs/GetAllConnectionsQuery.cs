using MediatR;
using NetPad.Data;

namespace NetPad.Apps.CQs;

public class GetAllConnectionsQuery : Query<GetAllConnectionsQuery.Response>
{
    public record Response(DataConnection[] Connections, DatabaseServerConnection[] Servers);

    public class Handler(IDataConnectionRepository dataConnectionRepository)
        : IRequestHandler<GetAllConnectionsQuery, Response>
    {
        public async Task<Response> Handle(GetAllConnectionsQuery request, CancellationToken cancellationToken)
        {
            var connections = await dataConnectionRepository.GetAllAsync();
            var servers = await dataConnectionRepository.GetAllServersAsync();

            return new Response(connections.ToArray(), servers.ToArray());
        }
    }
}
