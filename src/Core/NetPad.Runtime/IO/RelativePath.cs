using System.IO;

namespace NetPad.IO;

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
            PlatformUtil.IsOSWindows()
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture);

    public override int GetHashCode() => Path.ToLowerInvariant().GetHashCode();

    public static implicit operator RelativePath(string name) => new(name);

    public bool Exists() => File.Exists(Path);

    public string FullPath(DirectoryPath directoryPath) => System.IO.Path.Combine(directoryPath.Path, Path);
}
