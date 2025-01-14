using NetPad.Assemblies;

namespace NetPad.DotNet;

/// <summary>
/// Represents a file asset that belongs to a <see cref="Reference"/>. It may or may not be an assembly.
/// </summary>
/// <param name="Path">The absolute path to the asset.</param>
public record ReferenceAsset(string Path)
{
    private bool? _isManagedAssembly;

    public bool IsManagedAssembly => _isManagedAssembly ??= AssemblyInfoReader.IsManaged(Path);
}
