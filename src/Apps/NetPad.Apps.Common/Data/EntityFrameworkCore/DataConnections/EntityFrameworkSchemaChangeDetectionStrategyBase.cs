using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using NetPad.Data;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public abstract class EntityFrameworkSchemaChangeDetectionStrategyBase(
    IDataConnectionResourcesRepository dataConnectionResourcesRepository,
    IDataConnectionPasswordProtector passwordProtector)
{
    protected readonly IDataConnectionResourcesRepository _dataConnectionResourcesRepository = dataConnectionResourcesRepository;
    protected readonly IDataConnectionPasswordProtector _passwordProtector = passwordProtector;

    protected async Task ExecuteSqlCommandAsync(EntityFrameworkDatabaseConnection connection, string commandText, Func<DbDataReader, Task> process)
    {
        await using var context = connection.CreateDbContext(_passwordProtector);
        await using var command = context.Database.GetDbConnection().CreateCommand();

        command.CommandText = commandText;
        await context.Database.OpenConnectionAsync();

        await using var result = await command.ExecuteReaderAsync();

        await process(result);
    }
}
