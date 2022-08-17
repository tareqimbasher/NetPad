using System.Threading.Tasks;

namespace NetPad.Data;

public interface IDatabaseConnectionInfoProvider
{
    public Task<DatabaseStructure> GetDatabaseStructureAsync(DatabaseConnection databaseConnection);
}
