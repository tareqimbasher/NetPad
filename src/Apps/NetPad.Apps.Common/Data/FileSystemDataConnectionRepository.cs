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
public class FileSystemDataConnectionRepository : IDataConnectionRepository
{
    private readonly FilePath _connectionsFilePath = AppDataProvider.ConnectionsFilePath;

    private readonly JsonMigrationPipeline _fileMigrationPipeline = new([new DataConnectionFileV0ToV1MigrationStep()]);

    public async Task<IEnumerable<DataConnection>> GetAllAsync()
    {
        return (await LoadFileAsync()).Connections;
    }

    public async Task<DataConnection?> GetAsync(Guid id)
    {
        var file = await LoadFileAsync();
        return file.Connections.FirstOrDefault(x => x.Id == id);
    }

    public async Task SaveAsync(DataConnection connection)
    {
        var file = await LoadFileAsync();

        var index = file.Connections.FindIndex(x => x.Id == connection.Id);
        if (index == -1)
        {
            file.Connections.Add(connection);
        }
        else
        {
            file.Connections[index] = connection;
        }

        await SaveToFileAsync(file);
    }

    public async Task DeleteAsync(Guid id)
    {
        var file = await LoadFileAsync();
        var index = file.Connections.FindIndex(x => x.Id == id);

        if (index == -1)
        {
            return;
        }

        file.Connections.RemoveAt(index);
        await SaveToFileAsync(file);
    }

    private async Task<DataConnectionFileV1> LoadFileAsync()
    {
        if (!_connectionsFilePath.Exists())
        {
            return new DataConnectionFileV1();
        }

        var json = await File.ReadAllTextAsync(_connectionsFilePath.Path);
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

    private async Task SaveToFileAsync(DataConnectionFileV1 file)
    {
        var json = JsonSerializer.Serialize(file, true);
        await File.WriteAllTextAsync(_connectionsFilePath.Path, json);
    }
}
