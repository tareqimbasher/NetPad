using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace NetPad.IO;

/// <summary>
/// An absolute path to a directory.
/// </summary>
public record DirectoryPath(string Path) : AbsolutePath(Path)
{
    public virtual bool Equals(DirectoryPath? other) =>
        Path.Equals(other?.Path,
            PlatformUtil.IsOSWindows()
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture);

    public override int GetHashCode() => Path.ToLowerInvariant().GetHashCode();

    [return: NotNullIfNotNull("name")]
    public static implicit operator DirectoryPath?(string? name) => name is null ? null : new(name);

    public DirectoryInfo GetInfo() => new(Path);

    public DirectoryPath Combine(params string[] paths) => System.IO.Path.Combine(paths.Prepend(Path).ToArray());
    public FilePath CombineFilePath(params string[] paths) => System.IO.Path.Combine(paths.Prepend(Path).ToArray());

    public override bool Exists() => Directory.Exists(Path);

    public bool IsReadable() => FileSystemUtil.IsDirectoryReadable(Path);

    public bool IsWritable() => FileSystemUtil.IsDirectoryWritable(Path);

    public DirectoryPath CreateIfNotExists()
    {
        Directory.CreateDirectory(Path);
        return this;
    }

    public override void DeleteIfExists()
    {
        if (Exists())
        {
            Directory.Delete(Path, true);
        }
    }

    public static bool TryParse(string str, [NotNullWhen(true)] out DirectoryPath? dirPath)
    {
        try
        {
            dirPath = new DirectoryPath(str);
            return true;
        }
        catch
        {
            dirPath = null;
            return false;
        }
    }
}
