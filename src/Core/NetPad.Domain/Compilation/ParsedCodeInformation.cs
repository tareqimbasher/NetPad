using System.Collections.Generic;

namespace NetPad.Compilation;

public class ParsedCodeInformation
{
    public ParsedCodeInformation(
        string bootstrapperClassName,
        string bootstrapperSetIoMethodName)
    {
        BootstrapperClassName = bootstrapperClassName;
        BootstrapperSetIOMethodName = bootstrapperSetIoMethodName;
    }

    public string BootstrapperClassName { get; }
    public string BootstrapperSetIOMethodName { get; }
}
