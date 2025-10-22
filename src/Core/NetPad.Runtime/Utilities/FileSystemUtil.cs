using System.IO;

namespace NetPad.Utilities;

public static class FileSystemUtil
{
    /// <summary>
    /// Returns a human-readable string representation of the specified file size.
    /// </summary>
    /// <param name="bytes">The file size in bytes.</param>
    /// <param name="decimalPlaces">The number of decimal places to show in the output.</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="bytes"/> is less than 0.</exception>
    public static string GetReadableFileSize(long bytes, int decimalPlaces = 2)
    {
        if (bytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "File size cannot be negative.");
        }

        string[] sizeUnits = ["B ", "KB", "MB", "GB", "TB", "PB", "EB"];
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizeUnits.Length - 1)
        {
            order++;
            len /= 1024;
        }

        // Format with up to 2 decimal places, unless it's in bytes
        string format = "0." + new string('#', decimalPlaces);
        return $"{len.ToString(format)} {sizeUnits[order]}";
    }

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
