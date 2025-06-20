namespace NetPad.Data.Metadata;

public interface IDatabaseConnectionMetadataProviderFactory
{
    IDatabaseConnectionMetadataProvider Create(DatabaseConnection databaseConnection);
}
