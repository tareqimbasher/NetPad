using System.Text.Json;
using Microsoft.Extensions.Logging;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.IO;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Apps.Data;

public class FileSystemDataConnectionRepository(ILogger<FileSystemDataConnectionRepository> logger)
    : IDataConnectionRepository
{
    private readonly FilePath _connectionsFilePath = AppDataProvider.AppDataDirectoryPath.CombineFilePath("data-connections.json");

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

        var connections = new Dictionary<Guid, DataConnection>();

        try
        {
            var json = await File.ReadAllTextAsync(_connectionsFilePath.Path);

            var jsonDocument = JsonDocument.Parse(json);

            int itemIndex = -1;
            foreach (var o in jsonDocument.RootElement.EnumerateObject())
            {
                itemIndex++;
                Guid? connectionId = null;

                try
                {
                    if (!Guid.TryParse(o.Name, out var id))
                    {
                        logger.LogError("Could not deserialize data connection ID at index: {Index}. Data connections file: {Path}",
                            itemIndex,
                            _connectionsFilePath.Path);
                        continue;
                    }

                    connectionId = id;

                    var connection = o.Value.Deserialize<DataConnection>(JsonSerializer.DefaultOptions);

                    if (connection == null)
                    {
                        logger.LogError("Could not deserialize data connection at index: {Index}. Connection ID: {ConnectionId}. Data connections file: {Path}",
                            itemIndex,
                            connectionId,
                            _connectionsFilePath.Path);
                        continue;
                    }

                    connections.Add(connectionId.Value, connection);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Could not deserialize data connection at index: {Index}. Connection ID: {ConnectionId}. Data connections file: {Path}",
                        itemIndex,
                        connectionId,
                        _connectionsFilePath.Path);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while deserializing data connections file at: {Path}", _connectionsFilePath.Path);
        }

        return connections;
    }

    private async Task SaveToFileAsync(Dictionary<Guid, DataConnection> connections)
    {
        var json = JsonSerializer.Serialize(connections, true);
        await File.WriteAllTextAsync(_connectionsFilePath.Path, json);
    }
}
