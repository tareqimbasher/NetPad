using System.Collections.Generic;
using System.Threading.Tasks;
using NetPad.Compilation;
using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.Data;

public class DataConnectionResources
{
    public DataConnectionResources(DataConnection dataConnection)
    {
        DataConnection = dataConnection;
    }

    public DataConnection DataConnection { get; set; }
    public Task<SourceCodeCollection>? SourceCode { get; set; }
    public Task<byte[]?>? Assembly { get; set; }
    public Task<IEnumerable<Reference>>? RequiredReferences { get; set; }
}
