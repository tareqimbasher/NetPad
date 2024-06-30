using System.IO;

namespace NetPad.Utilities;

public static class FileSystemUtil
{
    /// <summary>
    /// Determines if a directory path is readable.
    /// </summary>
    /// <param name="path">The path to the directory to check.</param>
    /// <returns>Returns <see langword="true"/> if the directory exists and is readable, otherwise <see langword="false"/>.</returns>
    public static bool IsDirectoryReadable(string path)
    {
        try
        {
            _ = Directory.EnumerateFileSystemEntries(path).Any();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Determines if a directory path is writable.
    /// </summary>
    /// <param name="path">The path to the directory to check.</param>
    /// <returns>Returns <see langword="true"/> if the directory exists and is writable, otherwise <see langword="false"/>.</returns>
    public static bool IsDirectoryWritable(string path)
    {
        try
        {
            using var fs = File.Create(Path.Combine(path, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
