using NetPad.IO;

namespace NetPad.Apps;

internal static class FileBackup
{
    /// <summary>
    /// Copies a file to a Backups directory beside it. Returns the backup's path, or
    /// <see langword="null"/> if there was no file to back up.
    /// </summary>
    public static FilePath? Create(FilePath file)
    {
        if (!file.Exists())
        {
            return null;
        }

        var backupDirectory = Path.Combine(Path.GetDirectoryName(file.Path)!, "Backups");
        Directory.CreateDirectory(backupDirectory);

        var backupPath = new FilePath(Path.Combine(
            backupDirectory,
            $"{file.FileNameWithoutExtension}_backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{file.Extension}"));

        File.Copy(file.Path, backupPath.Path, true);

        return backupPath;
    }
}
