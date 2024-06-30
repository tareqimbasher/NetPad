namespace NetPad.Data;

public interface IDatabaseConnectionMetadataProvider
{
    Task<DatabaseStructure> GetDatabaseStructureAsync(DatabaseConnection databaseConnection);
}
