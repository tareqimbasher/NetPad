using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetPad.Common;

/// <summary>
/// A centralized JSON serializer that wraps System.Text.Json for use across the entire application.
/// Always use this serializer throughout the entire codebase unless there is a specific reason to
/// use a different one. This is to ensure consistent JSON formatting and behavior everywhere.
/// </summary>
public static class JsonSerializer
{
    static JsonSerializer()
    {
        DefaultOptions = Configure(new JsonSerializerOptions());
    }

    public static JsonSerializerOptions DefaultOptions { get; }

    /// <summary>
    /// Configures an instance of <see cref="JsonSerializerOptions"/> to use the same settings used in this serializer.
    /// </summary>
    public static JsonSerializerOptions Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public static string Serialize(object? value, JsonSerializerOptions options)
    {
        return System.Text.Json.JsonSerializer.Serialize(value, options);
    }

    public static string Serialize(object? value, bool indented = false)
    {
        var options = indented ? Configure(new JsonSerializerOptions { WriteIndented = true }) : DefaultOptions;
        return System.Text.Json.JsonSerializer.Serialize(value, options);
    }

    public static string Serialize(object? value, Type type, bool indented = false)
    {
        var options = indented ? Configure(new JsonSerializerOptions { WriteIndented = true }) : DefaultOptions;
        return System.Text.Json.JsonSerializer.Serialize(value, type, options);
    }

    public static T? Deserialize<T>(string json)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    public static T? Deserialize<T>(string json, JsonSerializerOptions options)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(json, options);
    }

    public static object? Deserialize(string json, Type returnType)
    {
        return System.Text.Json.JsonSerializer.Deserialize(json, returnType, DefaultOptions);
    }
}
