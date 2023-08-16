using NetPad.Plugins.OmniSharp.Features.Common.FileOperation;

namespace NetPad.Plugins.OmniSharp.Features.Rename;

public class RenameResponse
{
    public IEnumerable<ModifiedFileResponse>? Changes { get; set; }
    public string? ErrorMessage { get; set; }
}
