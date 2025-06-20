namespace NetPad.Data.Metadata;

public interface IDataConnectionResourcesGeneratorFactory
{
    IDataConnectionResourcesGenerator Create(DataConnection dataConnection);
}
