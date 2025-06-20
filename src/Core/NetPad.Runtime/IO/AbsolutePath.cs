namespace NetPad.IO;

/// <summary>
/// An absolute path to a file or a directory.
/// </summary>
public abstract record AbsolutePath
{
    /// <summary>
    /// The string representation of this path.
    /// </summary>
    public string Path { get; }

    public AbsolutePath(string path) =>
        Path =
            string.IsNullOrWhiteSpace(path)
                ? throw new ArgumentException("path cannot be null or empty", nameof(path))
                : System.IO.Path.GetInvalidPathChars().Intersect(path).Any()
                    ? throw new ArgumentException("path contains illegal characters", nameof(path))
                    : System.IO.Path.GetFullPath(path.Trim());

    // This method is sealed so derived records inherit it. Otherwise compiler will generate its own ToString() implementation for derived record types.
    public sealed override string ToString() => Path;
    public abstract bool Exists();
    public abstract void DeleteIfExists();
}
