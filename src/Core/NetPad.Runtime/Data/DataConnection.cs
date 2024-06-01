using System.Runtime.Serialization;
using NetPad.Common;
using Newtonsoft.Json;
using NJsonSchema.Converters;

namespace NetPad.Data;

// Only used for NSwag
[JsonConverter(typeof(JsonInheritanceConverter), "discriminator")]
[System.Text.Json.Serialization.JsonConverter(typeof(JsonInheritanceConverter<DataConnection>))]
[KnownType("GetKnownTypes")]
public abstract class DataConnection(Guid id, string name, DataConnectionType type)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public DataConnectionType Type { get; } = type;

    /// <summary>
    /// Tests if the connection is valid.
    /// </summary>
    /// <returns></returns>
    public abstract Task<DataConnectionTestResult> TestConnectionAsync(IDataConnectionPasswordProtector passwordProtector);

    public override string ToString()
    {
        return $"[{Id}] {Name}";
    }

    #region KnownTypes

    private static Type[] GetKnownTypes()
    {
        return DataConnectionKnownTypes.ScanForKnownTypes();
    }

    private static class DataConnectionKnownTypes
    {
        private static readonly object _lock = new();
        private static Type[]? _knownTypes;

        public static Type[] ScanForKnownTypes()
        {
            if (_knownTypes != null)
                return _knownTypes;

            Type[] knownTypes;

            lock (_lock)
            {
                if (_knownTypes != null)
                    return _knownTypes;

                var dataConnectionType = typeof(DataConnection);

                knownTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.GetName().Name?.StartsWith("NetPad.") == true)
                    .SelectMany(a => a.GetExportedTypes())
                    .Where(t => t.IsClass && !t.IsAbstract && dataConnectionType.IsAssignableFrom(t))
                    .Distinct()
                    .ToArray();

                _knownTypes = knownTypes;
            }

            return _knownTypes;
        }
    }

    #endregion
}
