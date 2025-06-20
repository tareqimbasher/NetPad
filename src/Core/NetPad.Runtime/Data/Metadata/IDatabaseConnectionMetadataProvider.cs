namespace NetPad.Data.Metadata;

public interface IDatabaseConnectionMetadataProvider
{
    Task<DatabaseStructure> GetDatabaseStructureAsync(DatabaseConnection databaseConnection);
}
