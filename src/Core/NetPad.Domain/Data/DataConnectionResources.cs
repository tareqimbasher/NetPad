using System.Collections.Generic;
using System.Threading.Tasks;
using NetPad.DotNet;

namespace NetPad.Data;

public class DataConnectionResources
{
    public DataConnectionResources(DataConnection dataConnection)
    {
        DataConnection = dataConnection;
    }

    public DataConnection DataConnection { get; }
    public Task<DataConnectionSourceCode>? SourceCode { get; set; }
    public Task<AssemblyImage?>? Assembly { get; set; }
    public Task<IEnumerable<Reference>>? RequiredReferences { get; set; }
}
