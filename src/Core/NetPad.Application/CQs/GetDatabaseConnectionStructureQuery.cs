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
        private readonly IDatabaseConnectionMetadataProviderFactory _databaseConnectionMetadataProviderFactory;

        public Handler(IDatabaseConnectionMetadataProviderFactory databaseConnectionMetadataProviderFactory)
        {
            _databaseConnectionMetadataProviderFactory = databaseConnectionMetadataProviderFactory;
        }

        public async Task<DatabaseStructure> Handle(GetDatabaseConnectionStructureQuery request, CancellationToken cancellationToken)
        {
            var metadataProvider = _databaseConnectionMetadataProviderFactory.Create(request.DatabaseConnection);

            return await metadataProvider.GetDatabaseStructureAsync(request.DatabaseConnection);
        }
    }
}
