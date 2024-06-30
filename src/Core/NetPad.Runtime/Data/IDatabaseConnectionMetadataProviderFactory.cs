namespace NetPad.Data;

public interface IDatabaseConnectionMetadataProviderFactory
{
    IDatabaseConnectionMetadataProvider Create(DatabaseConnection databaseConnection);
}
