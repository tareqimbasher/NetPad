using MediatR;
using NetPad.Data;

namespace NetPad.Apps.CQs;

public class GetDataConnectionQuery(Guid dataConnectionid) : Query<DataConnection?>
{
    public Guid DataConnectionid { get; } = dataConnectionid;

    public class Handler(IDataConnectionRepository dataConnectionRepository)
        : IRequestHandler<GetDataConnectionQuery, DataConnection?>
    {
        public async Task<DataConnection?> Handle(GetDataConnectionQuery request, CancellationToken cancellationToken)
        {
            return await dataConnectionRepository.GetAsync(request.DataConnectionid);
        }
    }
}
