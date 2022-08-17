using MediatR;
using NetPad.Data;

namespace NetPad.CQs;

public class GetDatabaseConnectionStructureQuery : Query<DatabaseStructure>
{
    public GetDatabaseConnectionStructureQuery(DatabaseConnection databaseConnection)
    {
        DatabaseConnection = databaseConnection;
    }

    public DatabaseConnection DatabaseConnection { get; }

    public class Handler : IRequestHandler<GetDatabaseConnectionStructureQuery, DatabaseStructure>
    {
        private readonly IDatabaseConnectionInfoProvider _databaseConnectionInfoProvider;

        public Handler(IDatabaseConnectionInfoProvider databaseConnectionInfoProvider)
        {
            _databaseConnectionInfoProvider = databaseConnectionInfoProvider;
        }

        public async Task<DatabaseStructure> Handle(GetDatabaseConnectionStructureQuery request, CancellationToken cancellationToken)
        {
            return await _databaseConnectionInfoProvider.GetDatabaseStructureAsync(request.DatabaseConnection);
        }
    }
}
