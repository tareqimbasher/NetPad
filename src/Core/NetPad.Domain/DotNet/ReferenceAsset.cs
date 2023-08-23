using System.Threading.Tasks;
using NetPad.Utilities;

namespace NetPad.DotNet;

/// <summary>
/// Represents an file asset that belongs to a <see cref="Reference"/>. It could be an assembly or not.
/// </summary>
/// <param name="Path">The absolute path to the asset.</param>
public record ReferenceAsset(string Path)
{
    public bool IsAssembly() => AssemblyUtil.IsAssembly(Path);
    public byte[] ReadAllBytes() => System.IO.File.ReadAllBytes(Path);
    public async Task<byte[]> ReadAllBytesAsync() => await System.IO.File.ReadAllBytesAsync(Path);
}
