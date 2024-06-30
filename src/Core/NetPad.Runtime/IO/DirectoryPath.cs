using System.IO;

namespace NetPad.IO;

public record DirectoryPath(string Path) : AbsolutePath(Path)
{
    public virtual bool Equals(DirectoryPath? other) =>
        Path.Equals(other?.Path,
            PlatformUtil.IsOSWindows()
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture);

    public override int GetHashCode() => Path.ToLowerInvariant().GetHashCode();

    public static implicit operator DirectoryPath(string name) => new(name);

    public DirectoryInfo GetInfo() => new(Path);

    public DirectoryPath Combine(params string[] paths) => System.IO.Path.Combine(paths.Prepend(Path).ToArray());
    public FilePath CombineFilePath(params string[] paths) => System.IO.Path.Combine(paths.Prepend(Path).ToArray());

    public override bool Exists() => Directory.Exists(Path);

    public bool IsReadable() => FileSystemUtil.IsDirectoryReadable(Path);

    public bool IsWritable() => FileSystemUtil.IsDirectoryWritable(Path);

    public override void DeleteIfExists()
    {
        var dir = GetInfo();

        if (dir.Exists)
        {
            dir.Delete(true);
        }
    }
}
