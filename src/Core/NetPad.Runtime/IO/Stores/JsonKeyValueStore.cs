using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.IO.Stores;

public class JsonKeyValueStore<TStore, TItem>(FilePath storeFilePath, IDataProtector dataProtector)
    where TStore : IKeyedItemsStore<TItem>, new()
    where TItem : class, IKeyedItem, new()
{
    private readonly object _fileLock = new();

    private static readonly JsonSerializerOptions _serializerOptions = JsonSerializer.Configure(new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    });

    public TItem[] List()
    {
        lock (_fileLock)
        {
            return ReadStoreFile().Items.Values.ToArray();
        }
    }

    public TItem? Get(string key)
    {
        lock (_fileLock)
        {
            var data = ReadStoreFile();
            data.Items.TryGetValue(key, out var item);
            return item;
        }
    }

    public string? GetUnprotectedRawStringValue(string key)
    {
        var item = Get(key);
        if (item == null)
        {
            throw new KeyNotFoundException($"No item found with key: {key}");
        }

        var value = item.Value;

        if (value == null)
        {
            return null;
        }

        if (item is IProtectableItem { IsProtected: true })
        {
            value = dataProtector.Unprotect(value);
        }

        return value;
    }

    public T? GetValue<T>(string key)
    {
        var value = GetUnprotectedRawStringValue(key);
        if (value == null)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value);
    }

    public void Save<T>(string key, T? value, bool? protect = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be empty", nameof(key));
        }

        lock (_fileLock)
        {
            var data = ReadStoreFile();

            if (!data.Items.TryGetValue(key, out var item))
            {
                item = new TItem
                {
                    Key = key
                };
                data.Items[key] = item;
            }

            string? valueStr = null;

            if (value != null)
            {
                valueStr = JsonSerializer.Serialize(value, _serializerOptions);

                if (item is IProtectableItem protectableItem)
                {
                    if (protect == true || (protect == null && protectableItem.IsProtected))
                    {
                        valueStr = dataProtector.Protect(valueStr);
                    }

                    if (protect != null)
                    {
                        protectableItem.IsProtected = protect.Value;
                    }
                }
            }

            item.Value = valueStr;
            item.UpdatedAtUtc = DateTime.UtcNow;

            WriteStoreFile(data);
        }
    }

    public bool Delete(string key)
    {
        lock (_fileLock)
        {
            var data = ReadStoreFile();
            if (data.Items.Remove(key))
            {
                WriteStoreFile(data);
                return true;
            }

            return false;
        }
    }

    public void Delete(Func<TItem, bool> predicate)
    {
        lock (_fileLock)
        {
            var data = ReadStoreFile();
            var delKeys = data.Items.Values.Where(predicate).Select(x => x.Key).ToArray();

            if (delKeys.Length == 0)
            {
                return;
            }

            foreach (var key in delKeys)
            {
                data.Items.Remove(key);
            }

            WriteStoreFile(data);
        }
    }

    private TStore ReadStoreFile()
    {
        if (!storeFilePath.Exists())
        {
            storeFilePath.GetInfo().Directory?.Create();
            File.WriteAllText(storeFilePath.Path, "{}");
            var fileData = new TStore();
            WriteStoreFile(fileData);
            return fileData;
        }

        return JsonSerializer.Deserialize<TStore>(File.ReadAllText(storeFilePath.Path))
               ?? throw new Exception($"Could read store file: {storeFilePath.Path}");
    }

    private void WriteStoreFile(TStore data)
    {
        File.WriteAllText(storeFilePath.Path, JsonSerializer.Serialize(data, _serializerOptions));
    }
}
