using NetPad.Plugins.OmniSharp.Features.Common.FileOperation;

namespace NetPad.Plugins.OmniSharp.Features.CodeActions;

public class RunCodeActionResponse
{
    public IEnumerable<FileOperationResponse?>? Changes { get; set; }
}
