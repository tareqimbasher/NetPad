using MediatR;
using NetPad.Data;

namespace NetPad.CQs;

public class GetDataConnectionsQuery : Query<DataConnection[]>
{
    public class Handler : IRequestHandler<GetDataConnectionsQuery, DataConnection[]>
    {
        private readonly IDataConnectionRepository _dataConnectionRepository;

        public Handler(IDataConnectionRepository dataConnectionRepository)
        {
            _dataConnectionRepository = dataConnectionRepository;
        }

        public async Task<DataConnection[]> Handle(GetDataConnectionsQuery request, CancellationToken cancellationToken)
        {
            return (await _dataConnectionRepository.GetAllAsync()).ToArray();
        }
    }
}
