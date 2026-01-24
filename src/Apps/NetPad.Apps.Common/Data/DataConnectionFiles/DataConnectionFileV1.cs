using NetPad.Common;
using NetPad.Data;

namespace NetPad.Apps.Data.DataConnectionFiles;

public class DataConnectionFileV1 : IVersionedJson
{
    public int Version => 1;
    public List<DataConnection> Connections { get; set; } = [];
}
