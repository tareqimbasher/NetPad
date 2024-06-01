using MediatR;
using NetPad.Data;

namespace NetPad.Apps.CQs;

public class GetDataConnectionsQuery : Query<DataConnection[]>
{
    public class Handler(IDataConnectionRepository dataConnectionRepository)
        : IRequestHandler<GetDataConnectionsQuery, DataConnection[]>
    {
        public async Task<DataConnection[]> Handle(GetDataConnectionsQuery request, CancellationToken cancellationToken)
        {
            return (await dataConnectionRepository.GetAllAsync()).ToArray();
        }
    }
}
