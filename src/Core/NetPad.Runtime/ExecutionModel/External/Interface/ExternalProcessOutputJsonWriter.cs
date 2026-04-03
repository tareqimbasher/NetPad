using System.Collections;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NetPad.Presentation;

namespace NetPad.ExecutionModel.External.Interface;

/// <summary>
/// Converts output emitted by the script (ex. using Dump() or Console.Write)
/// to NDJSON (newline-delimited JSON) and writes it to the main output.
/// </summary>
internal class ExternalProcessOutputJsonWriter(Func<string, Task> writeToMainOut, bool dumpRawJson, bool includeSql)
    : IExternalProcessOutputWriter
{
    private static readonly Lazy<Regex> _ansiColorsRegex = new(() => new Regex(@"\x1B\[[^@-~]*[@-~]"));
    private uint _resultOutputCounter;
    private uint _sqlOutputCounter;

    private static readonly JsonSerializerOptions _valueOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        MaxDepth = 10
    };

    public async Task WriteResultAsync(object? output, DumpOptions? options = null)
    {
        uint order = Interlocked.Increment(ref _resultOutputCounter);

        if (output is string str && str.StartsWith("\u001B[", StringComparison.Ordinal))
        {
            output = _ansiColorsRegex.Value.Replace(str, string.Empty);
        }

        var jsonLine = SerializeLine("result", order, options?.Title, output);

        if (dumpRawJson)
        {
            await writeToMainOut(jsonLine);
        }
        else
        {
            var scriptOutput = new ScriptOutput(ScriptOutputKind.Result, order, jsonLine, ScriptOutputFormat.Json);
            await writeToMainOut(NetPad.Common.JsonSerializer.Serialize(scriptOutput));
        }
    }

    public async Task WriteSqlAsync(object? output, DumpOptions? options = null)
    {
        if (!includeSql)
        {
            return;
        }

        uint order = Interlocked.Increment(ref _sqlOutputCounter);
        var jsonLine = SerializeLine("sql", order, title: null, output);

        if (dumpRawJson)
        {
            await writeToMainOut(jsonLine);
        }
        else
        {
            var scriptOutput = new ScriptOutput(ScriptOutputKind.Sql, order, jsonLine, ScriptOutputFormat.Json);
            await writeToMainOut(NetPad.Common.JsonSerializer.Serialize(scriptOutput));
        }
    }

    private static string SerializeLine(string type, uint order, string? title, object? value)
    {
        // Materialize IQueryable (e.g. EF Core queries) to a concrete list before serialization.
        // IQueryable types can't be serialized directly by System.Text.Json.
        if (value is IQueryable)
        {
            var list = new List<object?>();
            foreach (var item in (IEnumerable)value)
                list.Add(item);
            value = list;
        }

        string valueJson;
        try
        {
            valueJson = JsonSerializer.Serialize(value, _valueOptions);
        }
        catch
        {
            valueJson = JsonSerializer.Serialize(value?.ToString());
        }

        var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("type", type);
            writer.WriteNumber("order", order);
            if (title != null)
                writer.WriteString("title", title);
            writer.WritePropertyName("value");
            writer.WriteRawValue(valueJson);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
