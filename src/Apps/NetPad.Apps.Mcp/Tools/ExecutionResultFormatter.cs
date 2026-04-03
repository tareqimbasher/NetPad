using System.Text.Json;
using System.Text.RegularExpressions;
using NetPad.Apps.Mcp.Dtos;

namespace NetPad.Apps.Mcp.Tools;

/// <summary>
/// Formats <see cref="HeadlessRunResult"/> into a structured string suitable for AI consumption.
/// </summary>
internal static partial class ExecutionResultFormatter
{
    private const int MaxOutputLength = 100_000;

    public static string Format(HeadlessRunResult result)
    {
        var outputEntries = new List<object>();
        var totalLength = 0;

        foreach (var output in result.Output)
        {
            var entry = ExtractOutputEntry(output);
            var entryLength = entry.Body?.Length ?? 0;
            totalLength += entryLength;

            if (totalLength > MaxOutputLength)
            {
                var remaining = MaxOutputLength - (totalLength - entryLength);
                if (remaining > 0 && entry.Body != null)
                {
                    outputEntries.Add(entry with { Body = entry.Body[..remaining] });
                }

                var omitted = result.Output.Count - outputEntries.Count;
                if (omitted > 0)
                {
                    outputEntries.Add(new OutputEntry("result", $"[Output truncated — {omitted} more entries omitted]", "text"));
                }
                else
                {
                    outputEntries.Add(new OutputEntry("result", "[Output truncated]", "text"));
                }

                break;
            }

            outputEntries.Add(entry);
        }

        var formatted = new
        {
            result.Status,
            result.Success,
            result.DurationMs,
            Output = outputEntries,
            result.CompilationErrors,
            result.Error
        };

        return JsonSerializer.Serialize(formatted, JsonDefaults.IndentedOptions);
    }

    private static OutputEntry ExtractOutputEntry(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return new OutputEntry("result", element.ToString(), "text");
        }

        var kind = element.TryGetProperty("kind", out var kindProp)
            ? kindProp.GetString()?.ToLowerInvariant() ?? "result"
            : "result";

        var body = element.TryGetProperty("body", out var bodyProp)
            ? bodyProp.ToString()
            : element.ToString();

        var format = element.TryGetProperty("format", out var formatProp)
            ? formatProp.GetString()?.ToLowerInvariant() ?? "text"
            : "text";

        if (format == "html" && body != null)
        {
            body = StripHtml(body);
            format = "text";
        }

        return new OutputEntry(kind, body, format);
    }

    private static string StripHtml(string html)
    {
        // Replace <br/> and block-closing tags with newlines, then strip all remaining tags
        var text = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</(?:div|p|tr|li|h[1-6])>", "\n", RegexOptions.IgnoreCase);
        text = HtmlTagRegex().Replace(text, string.Empty);
        text = System.Net.WebUtility.HtmlDecode(text);
        return text.Trim();
    }

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    private record OutputEntry(string Kind, string? Body, string Format);
}
