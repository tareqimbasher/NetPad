using NetPad.Assemblies;

namespace NetPad.DotNet;

/// <summary>
/// Represents an file asset that belongs to a <see cref="Reference"/>. It could be an assembly or not.
/// </summary>
/// <param name="Path">The absolute path to the asset.</param>
public record ReferenceAsset(string Path)
{
    private bool? _isAssembly;

    public bool IsAssembly()
    {
        _isAssembly ??= AssemblyInfoReader.IsManaged(Path);

        return _isAssembly.Value;
    }
}
