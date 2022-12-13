using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.IO;

namespace NetPad.Data;

public class FileSystemDataConnectionRepository : IDataConnectionRepository
{
    private readonly Settings _settings;
    private readonly FilePath _connectionsFilePath;

    public FileSystemDataConnectionRepository(Settings settings)
    {
        _settings = settings;
        _connectionsFilePath = Path.Combine(Settings.AppDataFolderPath.Path, "data-connections.json");
    }

    public async Task<IEnumerable<DataConnection>> GetAllAsync()
    {
        return (await GetFromFileAsync()).Values;
    }

    public async Task<DataConnection?> GetAsync(Guid id)
    {
        (await GetFromFileAsync()).TryGetValue(id, out var connection);
        return connection;
    }

    public async Task SaveAsync(DataConnection connection)
    {
        var connections = await GetFromFileAsync();

        if (connections.ContainsKey(connection.Id))
        {
            connections[connection.Id] = connection;
        }
        else
        {
            connections.Add(connection.Id, connection);
        }

        await SaveToFileAsync(connections);
    }

    public async Task DeleteAsync(Guid id)
    {
        var connections = await GetFromFileAsync();

        if (!connections.ContainsKey(id))
        {
            return;
        }

        connections.Remove(id);

        await SaveToFileAsync(connections);
    }

    private async Task<Dictionary<Guid, DataConnection>> GetFromFileAsync()
    {
        if (!_connectionsFilePath.Exists())
        {
            return new Dictionary<Guid, DataConnection>();
        }

        var json = await File.ReadAllTextAsync(_connectionsFilePath.Path);
        return JsonSerializer.Deserialize<Dictionary<Guid, DataConnection>>(json)
               ?? new Dictionary<Guid, DataConnection>();
    }

    private async Task SaveToFileAsync(Dictionary<Guid, DataConnection> connections)
    {
        var json = JsonSerializer.Serialize(connections, true);
        await File.WriteAllTextAsync(_connectionsFilePath.Path, json);
    }
}
