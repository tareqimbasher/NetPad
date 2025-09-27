using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NetPad.Data.Metadata;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public abstract class EntityFrameworkSchemaChangeDetectionStrategyBase(
    IDataConnectionResourcesRepository dataConnectionResourcesRepository,
    IDataConnectionPasswordProtector passwordProtector)
{
    protected readonly IDataConnectionResourcesRepository DataConnectionResourcesRepository = dataConnectionResourcesRepository;

    protected async Task ExecuteSqlCommandAsync(EntityFrameworkDatabaseConnection connection, string commandText, Func<DbDataReader, Task> process)
    {
        await using var context = connection.CreateDbContext(passwordProtector);
        await using var command = context.Database.GetDbConnection().CreateCommand();

        command.CommandText = commandText;
        await context.Database.OpenConnectionAsync();

        await using var result = await command.ExecuteReaderAsync();

        await process(result);
    }

    protected static byte[] CalculateHash(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return [];
        }

        using var md5 = MD5.Create();
        return md5.ComputeHash(Encoding.UTF8.GetBytes(input));
    }
}
