using System;
using System.IO;
using System.Linq;
using NetPad.Utilities;

namespace NetPad.IO;

public record DirectoryPath
{
    public string Path { get; }

    public DirectoryPath(string path) =>
        Path =
            string.IsNullOrWhiteSpace(path)
                ? throw new ArgumentException("path cannot be null or empty")
                : System.IO.Path.GetInvalidPathChars().Intersect(path).Any()
                    ? throw new ArgumentException("Path contains illegal characters")
                    : System.IO.Path.GetFullPath(path.Trim());

    public override string ToString() => Path;

    public virtual bool Equals(DirectoryPath? other) =>
        Path.Equals(other?.Path,
            PlatformUtils.IsWindowsPlatform()
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture);

    public override int GetHashCode() => Path.ToLowerInvariant().GetHashCode();

    public static implicit operator DirectoryPath(string name) => new DirectoryPath(name);

    public DirectoryInfo GetInfo() => new DirectoryInfo(Path);

    public DirectoryPath Combine(params string[] paths) => System.IO.Path.Combine(paths.Prepend(Path).ToArray());
    public FilePath CombineFilePath(params string[] paths) => System.IO.Path.Combine(paths.Prepend(Path).ToArray());

    public bool Exists() => Directory.Exists(Path);
}

public record FilePath
{
    public string Path { get; }

    public FilePath(string path) =>
        Path =
            string.IsNullOrWhiteSpace(path)
                ? throw new ArgumentException("path cannot be null or empty")
                : System.IO.Path.GetInvalidPathChars().Intersect(path).Any() || System.IO.Path.GetInvalidFileNameChars().Intersect(System.IO.Path.GetFileName(path)).Any()
                    ? throw new ArgumentException($"Path {path} contains illegal characters")
                    : System.IO.Path.GetFullPath(path.Trim());

    public override string ToString() => Path;

    public virtual bool Equals(FilePath? other) =>
        Path.Equals(other?.Path,
            PlatformUtils.IsWindowsPlatform()
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture);

    public override int GetHashCode() => Path.ToLowerInvariant().GetHashCode();

    public static implicit operator FilePath(string name) => new FilePath(name);

    public FileInfo GetInfo() => new FileInfo(Path);

    public FilePath Combine(params string[] paths) => System.IO.Path.Combine(paths.Prepend(Path).ToArray());

    public bool Exists() => File.Exists(Path);
}
