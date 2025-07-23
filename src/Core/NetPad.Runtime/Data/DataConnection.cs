using System.Runtime.Serialization;
using NetPad.Data.Security;
using Newtonsoft.Json;
using NJsonSchema.Converters;

namespace NetPad.Data;

/// <summary>
/// A connection to a data source (ex. a database, a flat file...etc.)
/// </summary>
// Only used for NSwag
[JsonConverter(typeof(JsonInheritanceConverter<DataConnection>), "discriminator")]
[System.Text.Json.Serialization.JsonConverter(typeof(Common.JsonInheritanceConverter<DataConnection>))]
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
    public abstract Task<DataConnectionTestResult> TestConnectionAsync(
        IDataConnectionPasswordProtector passwordProtector);

    public override string ToString()
    {
        return $"[{Id}] {Name}";
    }

    #region KnownTypes

    private static Type[] GetKnownTypes()
    {
        return DataConnectionKnownTypes.KnownTypes.Value;
    }

    private static class DataConnectionKnownTypes
    {
        public static readonly Lazy<Type[]> KnownTypes = new(() =>
        {
            var dataConnectionType = typeof(DataConnection);

            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => x.GetName().FullName.Contains("NetPad"))
                .SelectMany(x => x.GetExportedTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false } && dataConnectionType.IsAssignableFrom(t))
                .ToArray();
        });
    }

    #endregion
}
