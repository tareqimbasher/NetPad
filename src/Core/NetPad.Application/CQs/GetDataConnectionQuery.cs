using MediatR;
using NetPad.Data;

namespace NetPad.CQs;

public class GetDataConnectionQuery : Query<DataConnection?>
{
    public GetDataConnectionQuery(Guid dataConnectionid)
    {
        DataConnectionid = dataConnectionid;
    }

    public Guid DataConnectionid { get; }

    public class Handler : IRequestHandler<GetDataConnectionQuery, DataConnection?>
    {
        private readonly IDataConnectionRepository _dataConnectionRepository;

        public Handler(IDataConnectionRepository dataConnectionRepository)
        {
            _dataConnectionRepository = dataConnectionRepository;
        }

        public async Task<DataConnection?> Handle(GetDataConnectionQuery request, CancellationToken cancellationToken)
        {
            return await _dataConnectionRepository.GetAsync(request.DataConnectionid);
        }
    }
}
