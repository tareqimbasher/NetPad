using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;

namespace NetPad.Apps.Mcp.Tools;

/// <summary>
/// Formats <see cref="HeadlessRunResult"/> into a structured string suitable for AI consumption.
/// </summary>
internal static class ExecutionResultFormatter
{
    private const int MaxOutputLength = 100_000;

    public static string Format(HeadlessRunResult result)
    {
        var outputTexts = new List<string>();
        var totalLength = 0;

        foreach (var output in result.Output)
        {
            var text = ExtractOutputText(output);
            totalLength += text.Length;

            if (totalLength > MaxOutputLength)
            {
                var remaining = MaxOutputLength - (totalLength - text.Length);
                if (remaining > 0)
                {
                    outputTexts.Add(text[..remaining]);
                }

                var omitted = result.Output.Count - outputTexts.Count;
                if (omitted > 0)
                {
                    outputTexts.Add($"[Output truncated — {omitted} more entries omitted]");
                }
                else
                {
                    outputTexts.Add("[Output truncated]");
                }

                break;
            }

            outputTexts.Add(text);
        }

        var formatted = new
        {
            result.Status,
            result.Success,
            result.DurationMs,
            Output = outputTexts,
            result.CompilationErrors,
            result.Error
        };

        return JsonSerializer.Serialize(formatted);
    }

    private static string ExtractOutputText(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? string.Empty;
        }

        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("body", out var body))
        {
            return body.ToString();
        }

        return element.ToString();
    }
}
