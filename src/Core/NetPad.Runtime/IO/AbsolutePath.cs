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

    // This method is sealed so derived records inherit it. Otherwise compiler will generate its own ToString() implementation for derived record types.
    public sealed override string ToString() => Path;
    public abstract bool Exists();
    public abstract void DeleteIfExists();
}
