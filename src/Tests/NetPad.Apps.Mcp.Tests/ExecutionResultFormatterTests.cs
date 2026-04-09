using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests;

public class ExecutionResultFormatterTests
{
    [Fact]
    public void Format_SuccessfulResult_IncludesAllFields()
    {
        var result = new HeadlessRunResult
        {
            Status = "completed",
            Success = true,
            DurationMs = 42.5,
            Output = [JsonElement("Hello, World!")]
        };

        var formatted = ExecutionResultFormatter.Format(result);
        var doc = JsonDocument.Parse(formatted);
        var root = doc.RootElement;

        Assert.Equal("completed", root.GetProperty("Status").GetString());
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(42.5, root.GetProperty("DurationMs").GetDouble());

        var entry = root.GetProperty("Output")[0];
        Assert.Equal("result", entry.GetProperty("Kind").GetString());
        Assert.Equal("Hello, World!", entry.GetProperty("Body").GetString());
        Assert.Equal("text", entry.GetProperty("Format").GetString());
    }

    [Fact]
    public void Format_WithCompilationErrors_IncludesErrors()
    {
        var result = new HeadlessRunResult
        {
            Status = "failed",
            Success = false,
            DurationMs = 10,
            CompilationErrors = ["CS1002: ; expected", "CS0103: Name 'x' does not exist"],
            Error = "Compilation failed"
        };

        var formatted = ExecutionResultFormatter.Format(result);
        var doc = JsonDocument.Parse(formatted);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Compilation failed", root.GetProperty("Error").GetString());

        var errors = root.GetProperty("CompilationErrors");
        Assert.Equal(2, errors.GetArrayLength());
        Assert.Equal("CS1002: ; expected", errors[0].GetString());
    }

    [Fact]
    public void Format_StringOutput_ExtractsText()
    {
        var result = new HeadlessRunResult
        {
            Status = "completed",
            Success = true,
            Output = [JsonElement("line 1"), JsonElement("line 2")]
        };

        var formatted = ExecutionResultFormatter.Format(result);
        var doc = JsonDocument.Parse(formatted);
        var output = doc.RootElement.GetProperty("Output");

        Assert.Equal(2, output.GetArrayLength());
        Assert.Equal("line 1", output[0].GetProperty("Body").GetString());
        Assert.Equal("line 2", output[1].GetProperty("Body").GetString());
    }

    [Fact]
    public void Format_ObjectOutputWithBody_NoFormat_PreservesBodyAsText()
    {
        // When no "format" property is present, format defaults to "text" and body is kept as-is
        var obj = new { body = "<table><tr><td>Data</td></tr></table>", title = "Results" };
        var result = new HeadlessRunResult
        {
            Status = "completed",
            Success = true,
            Output = [JsonElement(obj)]
        };

        var formatted = ExecutionResultFormatter.Format(result);
        var doc = JsonDocument.Parse(formatted);
        var output = doc.RootElement.GetProperty("Output");

        Assert.Single(output.EnumerateArray());
        var entry = output[0];
        Assert.Equal("result", entry.GetProperty("Kind").GetString());
        Assert.Equal("text", entry.GetProperty("Format").GetString());
        Assert.Contains("<table>", entry.GetProperty("Body").GetString());
    }

    [Fact]
    public void Format_HtmlFormatOutput_StripsHtmlTags()
    {
        // When format is "html", the formatter strips HTML tags and converts to plain text
        var obj = new { body = "<table><tr><td>Data</td></tr></table>", format = "html" };
        var result = new HeadlessRunResult
        {
            Status = "completed",
            Success = true,
            Output = [JsonElement(obj)]
        };

        var formatted = ExecutionResultFormatter.Format(result);
        var doc = JsonDocument.Parse(formatted);
        var output = doc.RootElement.GetProperty("Output");

        var entry = output[0];
        Assert.Equal("text", entry.GetProperty("Format").GetString());
        Assert.Contains("Data", entry.GetProperty("Body").GetString());
        Assert.DoesNotContain("<table>", entry.GetProperty("Body").GetString()!);
    }

    [Fact]
    public void Format_LongOutput_Truncates()
    {
        // Create output that exceeds the 100,000 character limit
        var longText = new string('x', 50_000);
        var result = new HeadlessRunResult
        {
            Status = "completed",
            Success = true,
            Output = [JsonElement(longText), JsonElement(longText), JsonElement(longText)]
        };

        var formatted = ExecutionResultFormatter.Format(result);
        var doc = JsonDocument.Parse(formatted);
        var output = doc.RootElement.GetProperty("Output");

        // Should have truncated — look for the truncation marker in the last entry's Body
        var lastEntry = output[output.GetArrayLength() - 1];
        var body = lastEntry.GetProperty("Body").GetString()!;
        Assert.Contains("truncated", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_EmptyOutput_ReturnsEmptyArray()
    {
        var result = new HeadlessRunResult
        {
            Status = "completed",
            Success = true,
            Output = []
        };

        var formatted = ExecutionResultFormatter.Format(result);
        var doc = JsonDocument.Parse(formatted);
        var output = doc.RootElement.GetProperty("Output");

        Assert.Equal(0, output.GetArrayLength());
    }

    [Fact]
    public void Format_NumberOutput_ConvertsToString()
    {
        var result = new HeadlessRunResult
        {
            Status = "completed",
            Success = true,
            Output = [JsonElement(42)]
        };

        var formatted = ExecutionResultFormatter.Format(result);
        var doc = JsonDocument.Parse(formatted);
        var output = doc.RootElement.GetProperty("Output");

        var entry = output[0];
        Assert.Equal("result", entry.GetProperty("Kind").GetString());
        Assert.Equal("42", entry.GetProperty("Body").GetString());
        Assert.Equal("text", entry.GetProperty("Format").GetString());
    }

    private static JsonElement JsonElement(object value)
    {
        var json = JsonSerializer.Serialize(value);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
