using System.Collections.Generic;

namespace NetPad.Compilation;

public class ParsedCodeInformation
{
    public ParsedCodeInformation(
        int userCodeStartLine,
        HashSet<string> namespaces,
        string bootstrapperClassName,
        string bootstrapperSetIoMethodName)
    {
        UserCodeStartLine = userCodeStartLine;
        Namespaces = namespaces;
        BootstrapperClassName = bootstrapperClassName;
        BootstrapperSetIOMethodName = bootstrapperSetIoMethodName;
    }

    public int UserCodeStartLine { get; }
    public HashSet<string> Namespaces { get; }
    public string BootstrapperClassName { get; }
    public string BootstrapperSetIOMethodName { get; }
}
