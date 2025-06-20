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

    /// <summary>
    /// Copies a directory. This method will throw an exception when copying a file and the file already exists
    /// in the destination.
    /// </summary>
    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        }

        Directory.CreateDirectory(destinationDir);

        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        if (recursive)
        {
            var subDirs = dir.GetDirectories();

            foreach (var subDir in subDirs)
            {
                var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}
