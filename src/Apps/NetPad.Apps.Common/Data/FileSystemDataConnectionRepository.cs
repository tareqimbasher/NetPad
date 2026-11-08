using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using NetPad.Apps.Data.DataConnectionFiles;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.IO;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Apps.Data;

/// <summary>
/// An implementation of <see cref="IDataConnectionRepository"/> that persists data connections to the local file system.
/// </summary>
public class FileSystemDataConnectionRepository(
    ILogger<FileSystemDataConnectionRepository> logger,
    FilePath? connectionsFilePath = null) : IDataConnectionRepository
{
    private readonly FilePath _connectionsFilePath = connectionsFilePath ?? AppDataProvider.ConnectionsFilePath;

    private readonly JsonMigrationPipeline _fileMigrationPipeline = new([new DataConnectionFileV0ToV1MigrationStep()]);

    // Not re-entrant, so callbacks passed to ReadFileAsync and UpdateFileAsync must not call this class's public methods.
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Task<IEnumerable<DataConnection>> GetAllAsync()
    {
        return ReadFileAsync<IEnumerable<DataConnection>>(file => file.Connections);
    }

    public Task<DataConnection?> GetAsync(Guid id)
    {
        return ReadFileAsync(file => file.Connections.FirstOrDefault(x => x.Id == id));
    }

    public Task SaveAsync(DataConnection connection)
    {
        return UpdateFileAsync(file =>
        {
            var index = file.Connections.FindIndex(x => x.Id == connection.Id);
            if (index == -1)
            {
                file.Connections.Add(connection);
            }
            else
            {
                file.Connections[index] = connection;
            }

            return true;
        });
    }

    public Task DeleteAsync(Guid id)
    {
        return UpdateFileAsync(file =>
        {
            var index = file.Connections.FindIndex(x => x.Id == id);

            if (index == -1)
            {
                return false;
            }

            file.Connections.RemoveAt(index);
            return true;
        });
    }

    public Task<IEnumerable<DatabaseServerConnection>> GetAllServersAsync()
    {
        return ReadFileAsync<IEnumerable<DatabaseServerConnection>>(file => file.DatabaseServers);
    }

    public Task<DatabaseServerConnection?> GetServerAsync(Guid id)
    {
        return ReadFileAsync(file => file.DatabaseServers.FirstOrDefault(x => x.Id == id));
    }

    public Task SaveServerAsync(DatabaseServerConnection server)
    {
        return UpdateFileAsync(file =>
        {
            var index = file.DatabaseServers.FindIndex(x => x.Id == server.Id);
            if (index == -1)
            {
                file.DatabaseServers.Add(server);
            }
            else
            {
                file.DatabaseServers[index] = server;
            }

            return true;
        });
    }

    public Task DeleteServerAsync(Guid id)
    {
        return UpdateFileAsync(file =>
        {
            var index = file.DatabaseServers.FindIndex(x => x.Id == id);

            if (index == -1)
            {
                return false;
            }

            file.DatabaseServers.RemoveAt(index);

            // Cascade-delete connections attached to this server
            file.Connections.RemoveAll(c => c is DatabaseConnection dc && dc.ServerId == id);

            return true;
        });
    }

    private async Task<T> ReadFileAsync<T>(Func<DataConnectionFileV1, T> read)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            return read(await LoadFileAsync().ConfigureAwait(false));
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <param name="update">Changes the file's contents and returns whether there is anything to save.</param>
    private async Task UpdateFileAsync(Func<DataConnectionFileV1, bool> update)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            var file = await LoadFileAsync().ConfigureAwait(false);

            if (update(file))
            {
                await SaveToFileAsync(file).ConfigureAwait(false);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<DataConnectionFileV1> LoadFileAsync()
    {
        if (!_connectionsFilePath.Exists())
        {
            return new DataConnectionFileV1();
        }

        var json = await File.ReadAllTextAsync(_connectionsFilePath.Path).ConfigureAwait(false);

        if (TryParse(json) == null)
        {
            return await ResetUnparseableFileAsync().ConfigureAwait(false);
        }

        var file = _fileMigrationPipeline.MigrateToLatest<DataConnectionFileV1>(json, JsonSerializer.DefaultOptions);

        // Hydrate server references
        var serverMap = file.DatabaseServers.ToDictionary(s => s.Id);
        foreach (var conn in file.Connections.OfType<DatabaseConnection>())
        {
            if (conn.ServerId is { } serverId && serverMap.TryGetValue(serverId, out var server))
                conn.SetServer(server);
        }

        return file;
    }

    private async Task<DataConnectionFileV1> ResetUnparseableFileAsync()
    {
        var backupPath = FileBackup.Create(_connectionsFilePath);
        logger.LogError(
            "Data connections file could not be parsed and was reset to empty. The original was backed up to: {BackupPath}",
            backupPath?.Path);

        var file = new DataConnectionFileV1();
        await SaveToFileAsync(file).ConfigureAwait(false);
        return file;
    }

    private static JsonObject? TryParse(string json)
    {
        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task SaveToFileAsync(DataConnectionFileV1 file)
    {
        // Ensure connections reference the latest server instances before serializing
        var serverMap = file.DatabaseServers.ToDictionary(s => s.Id);
        foreach (var conn in file.Connections.OfType<DatabaseConnection>())
        {
            if (conn.ServerId is { } serverId && serverMap.TryGetValue(serverId, out var server))
                conn.SetServer(server);
        }

        var json = JsonSerializer.Serialize(file, true);

        var tempPath = $"{_connectionsFilePath.Path}.{Guid.NewGuid():N}.tmp";

        try
        {
            await File.WriteAllTextAsync(tempPath, json).ConfigureAwait(false);
            File.Move(tempPath, _connectionsFilePath.Path, overwrite: true);
        }
        catch
        {
            File.Delete(tempPath);
            throw;
        }
    }
}
