using System;
using System.IO;
using System.Linq;

namespace NetPad.IO;

public abstract record AbsolutePath
{
    public string Path { get; }

    public AbsolutePath(string path) =>
        Path =
            string.IsNullOrWhiteSpace(path)
                ? throw new ArgumentException("path cannot be null or empty")
                : System.IO.Path.GetInvalidPathChars().Intersect(path).Any()
                    ? throw new ArgumentException("Path contains illegal characters")
                    : System.IO.Path.GetFullPath(path.Trim());

    public override string ToString() => Path;
    public abstract bool Exists();
    public abstract void DeleteIfExists();
}

public record DirectoryPath(string Path) : AbsolutePath(Path)
{
    public virtual bool Equals(DirectoryPath? other) =>
        Path.Equals(other?.Path,
            PlatformUtil.IsWindowsPlatform()
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture);

    public override int GetHashCode() => Path.ToLowerInvariant().GetHashCode();

    public static implicit operator DirectoryPath(string name) => new(name);

    public DirectoryInfo GetInfo() => new(Path);

    public DirectoryPath Combine(params string[] paths) => System.IO.Path.Combine(paths.Prepend(Path).ToArray());
    public FilePath CombineFilePath(params string[] paths) => System.IO.Path.Combine(paths.Prepend(Path).ToArray());

    public override bool Exists() => Directory.Exists(Path);

    public override void DeleteIfExists()
    {
        var dir = GetInfo();
        if (dir.Exists) dir.Delete(true);
    }
}

public record FilePath(string Path) : AbsolutePath(Path)
{
    public virtual bool Equals(FilePath? other) =>
        Path.Equals(other?.Path,
            PlatformUtil.IsWindowsPlatform()
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture);

    public override int GetHashCode() => Path.ToLowerInvariant().GetHashCode();

    public static implicit operator FilePath(string name) => new(name);

    public FileInfo GetInfo() => new(Path);

    public FilePath Combine(params string[] paths) => System.IO.Path.Combine(paths.Prepend(Path).ToArray());

    public override bool Exists() => File.Exists(Path);

    public override void DeleteIfExists()
    {
        var file = GetInfo();
        if (file.Exists) file.Delete();
    }
}

public record RelativePath
{
    public RelativePath(string path) =>
        Path =
            string.IsNullOrWhiteSpace(path)
                ? throw new ArgumentException("path cannot be null or empty")
                : System.IO.Path.GetInvalidPathChars().Distinct().Intersect(path).Any() ||
                  System.IO.Path.GetInvalidFileNameChars().Intersect(System.IO.Path.GetFileName(path)).Any()
                    ? throw new ArgumentException($"Path {path} contains illegal characters")
                    : path.Trim();

    public string Path { get; }

    public override string ToString() => Path;

    public virtual bool Equals(RelativePath? other) =>
        Path.Equals(other?.Path,
            PlatformUtil.IsWindowsPlatform()
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture);

    public override int GetHashCode() => Path.ToLowerInvariant().GetHashCode();

    public static implicit operator RelativePath(string name) => new(name);

    public bool Exists() => File.Exists(Path);

    public string FullPath(DirectoryPath directoryPath) => System.IO.Path.Combine(directoryPath.Path, Path);
}
