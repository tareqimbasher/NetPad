using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace NetPad.IO;

/// <summary>
/// An absolute path to a file.
/// </summary>
public record FilePath(string Path) : AbsolutePath(Path)
{
    public virtual bool Equals(FilePath? other) =>
        Path.Equals(other?.Path,
            PlatformUtil.IsOSWindows()
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture);

    public override int GetHashCode() => Path.ToLowerInvariant().GetHashCode();

    [return: NotNullIfNotNull("name")]
    public static implicit operator FilePath?(string? name) => name is null ? null : new(name);

    public string FileName => System.IO.Path.GetFileName(Path);

    public string FileNameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(Path);

    public string Extension => System.IO.Path.GetExtension(Path);

    public FileInfo GetInfo() => new(Path);

    public override bool Exists() => File.Exists(Path);

    public override void DeleteIfExists()
    {
        if (Exists())
        {
            File.Delete(Path);
        }
    }
}
