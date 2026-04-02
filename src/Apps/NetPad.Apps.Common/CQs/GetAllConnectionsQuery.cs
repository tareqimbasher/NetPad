using MediatR;
using NetPad.Data;

namespace NetPad.Apps.CQs;

public class GetAllConnectionsQuery : Query<GetAllConnectionsQuery.GetAllConnectionsResponse>
{
    public record GetAllConnectionsResponse(DataConnection[] Connections, DatabaseServerConnection[] Servers);

    public class Handler(IDataConnectionRepository dataConnectionRepository)
        : IRequestHandler<GetAllConnectionsQuery, GetAllConnectionsResponse>
    {
        public async Task<GetAllConnectionsResponse> Handle(GetAllConnectionsQuery request, CancellationToken cancellationToken)
        {
            var connections = await dataConnectionRepository.GetAllAsync();
            var servers = await dataConnectionRepository.GetAllServersAsync();

            return new GetAllConnectionsResponse(connections.ToArray(), servers.ToArray());
        }
    }
}
