using MediatR;
using NetPad.Data;

namespace NetPad.Apps.CQs;

public class GetDatabaseConnectionStructureQuery(DatabaseConnection databaseConnection) : Query<DatabaseStructure>
{
    public DatabaseConnection DatabaseConnection { get; } = databaseConnection;

    public class Handler(IDatabaseConnectionMetadataProviderFactory databaseConnectionMetadataProviderFactory)
        : IRequestHandler<GetDatabaseConnectionStructureQuery, DatabaseStructure>
    {
        public async Task<DatabaseStructure> Handle(GetDatabaseConnectionStructureQuery request, CancellationToken cancellationToken)
        {
            var metadataProvider = databaseConnectionMetadataProviderFactory.Create(request.DatabaseConnection);

            return await metadataProvider.GetDatabaseStructureAsync(request.DatabaseConnection);
        }
    }
}
