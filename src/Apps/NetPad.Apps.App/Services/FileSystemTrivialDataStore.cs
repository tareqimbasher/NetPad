using System.IO;
using System.Linq;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.IO;

namespace NetPad.Services;

/// <summary>
/// An implementation of <see cref="ITrivialDataStore"/> that persists trivial data to the local file system.
/// </summary>
public class FileSystemTrivialDataStore : ITrivialDataStore
{
    private static readonly FilePath _storeFilePath = AppDataProvider.AppDataDirectoryPath.CombineFilePath("key-values.txt");
    private static readonly object _fileLock = new();

    public TValue? Get<TValue>(string key) where TValue : class
    {
        lock (_fileLock)
        {
            if (!_storeFilePath.Exists())
            {
                return null;
            }

            using var reader = File.OpenText(_storeFilePath.Path);
            while (reader.ReadLine() is { } line)
            {
                if (line.Length == 0 || !line.StartsWith(key))
                {
                    continue;
                }

                var parts = line.Split('=', 2);
                if (parts.Length < 2)
                {
                    continue;
                }

                return JsonSerializer.Deserialize<TValue>(parts[1].Trim());
            }
        }

        return null;
    }

    public void Set<TValue>(string key, TValue value)
    {
        lock (_fileLock)
        {
            var lines = _storeFilePath.Exists()
                ? File.ReadAllLines(_storeFilePath.Path).ToList()
                : [];

            var iLine = lines.FindIndex(l => l.StartsWith(key));

            if (iLine >= 0)
            {
                lines[iLine] = $"{key}={JsonSerializer.Serialize(value)}";
            }
            else
            {
                lines.Add($"{key}={JsonSerializer.Serialize(value)}");
            }

            File.WriteAllLines(_storeFilePath.Path, lines);
        }
    }

    public bool Contains(string key)
    {
        lock (_fileLock)
        {
            if (!_storeFilePath.Exists())
            {
                return false;
            }

            using var reader = File.OpenText(_storeFilePath.Path);
            while (reader.ReadLine() is { } line)
            {
                if (line.Length == 0 || !line.StartsWith(key))
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }
}
