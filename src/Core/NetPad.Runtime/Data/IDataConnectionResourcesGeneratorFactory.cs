namespace NetPad.Data;

public interface IDataConnectionResourcesGeneratorFactory
{
    IDataConnectionResourcesGenerator Create(DataConnection dataConnection);
}
