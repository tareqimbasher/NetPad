using System.Text.Json;
using Microsoft.Extensions.Logging;
using NetPad.IO;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Runtimes;

public partial class ExternalProcessScriptRuntime
{
    public void AddInput(IInputReader<string> inputReader)
    {
        _externalInputReaders.Add(inputReader);
    }

    public void RemoveInput(IInputReader<string> inputReader)
    {
        _externalInputReaders.Remove(inputReader);
    }

    public void AddOutput(IOutputWriter<object> outputWriter)
    {
        _externalOutputWriters.Add(outputWriter);
    }

    public void RemoveOutput(IOutputWriter<object> outputWriter)
    {
        _externalOutputWriters.Remove(outputWriter);
    }

    /// <summary>
    /// Processes raw external process output data.
    /// </summary>
    /// <param name="raw">Raw output data as written to STD OUT of external process.</param>
    /// <exception cref="FormatException"></exception>
    private async Task OnProcessOutputReceived(string raw)
    {
        if (raw == "[INPUT_REQUEST]")
        {
            // The first reader that returns a non-null input will be used
            string? input = null;
            foreach (var inputReader in _externalInputReaders)
            {
                input = await inputReader.ReadAsync();
                if (input != null)
                {
                    break;
                }
            }

            var stdInput = _processHandler?.IO.StandardInput;

            if (stdInput != null)
            {
                await stdInput.WriteLineAsync(input);
            }

            return;
        }

        string type;
        JsonElement outputProperty;

        try
        {
            var json = JsonDocument.Parse(raw).RootElement;
            type = json.GetProperty(nameof(ExternalProcessOutput.Type).ToLowerInvariant()).GetString() ?? string.Empty;
            outputProperty = json.GetProperty(nameof(ExternalProcessOutput.Output).ToLowerInvariant());
        }
        catch
        {
            _logger.LogDebug("Script output is not JSON or could not be deserialized. Output: '{RawOutput}'", raw);
            _rawOutputHandler.RawErrorReceived(raw);
            return;
        }

        ScriptOutput output;

        if (type == nameof(HtmlResultsScriptOutput))
        {
            output = JsonSerializer.Deserialize<HtmlResultsScriptOutput>(outputProperty.ToString())
                     ?? throw new FormatException($"Could deserialize JSON to {nameof(HtmlResultsScriptOutput)}");
        }
        else if (type == nameof(HtmlSqlScriptOutput))
        {
            output = JsonSerializer.Deserialize<HtmlSqlScriptOutput>(outputProperty.ToString())
                     ?? throw new FormatException($"Could deserialize JSON to {nameof(HtmlSqlScriptOutput)}");
        }
        else if (type == nameof(HtmlErrorScriptOutput))
        {
            output = JsonSerializer.Deserialize<HtmlErrorScriptOutput>(outputProperty.ToString())
                     ?? throw new FormatException($"Could deserialize JSON to {nameof(HtmlErrorScriptOutput)}");
        }
        else
        {
            _rawOutputHandler.RawOutputReceived(raw);
            return;
        }

        await _output.WriteAsync(output);
    }

    /// <summary>
    /// Processes raw external process error data.
    /// </summary>
    /// <param name="raw">Raw error data as written to STD OUT of external process.</param>
    /// <param name="userProgramStartLineNumber">The line number the user's program starts. Used to correct line numbers.</param>
    private Task OnProcessErrorReceived(string raw, int userProgramStartLineNumber)
    {
        raw = CorrectUncaughtExceptionStackTraceLineNumber(raw, userProgramStartLineNumber);

        _rawOutputHandler.RawErrorReceived(raw);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Corrects line numbers in stack trace messages of uncaught exceptions in external running process.
    /// </summary>
    private static string CorrectUncaughtExceptionStackTraceLineNumber(string output, int userProgramStartLineNumber)
    {
        if (!output.StartsWith("   at ") || !output.Contains(" :line "))
        {
            return output;
        }

        var lineNumberStr = output.Split(" :line ").LastOrDefault();
        if (int.TryParse(lineNumberStr, out int lineNumber))
        {
            output = output[..^lineNumberStr.Length] + (lineNumber - userProgramStartLineNumber);
        }

        return output;
    }
}
