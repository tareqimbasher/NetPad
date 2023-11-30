using NetPad.IO;
using NetPad.Presentation;

namespace NetPad.Runtimes;

/// <summary>
/// Provides IO utils to a running script.
/// </summary>
public static class ScriptRuntimeServices
{
    // Capture default IO used by system to read/write
    private static readonly TextReader _defaultConsoleInput = Console.In;
    private static readonly TextWriter _defaultConsoleOutput = Console.Out;

    static ScriptRuntimeServices()
    {
        Output = new ExternalProcessOutputHtmlWriter(str => _defaultConsoleOutput.WriteLineAsync(str));
    }

    internal static IExternalProcessOutputWriter Output { get; private set; }
    internal static ExternalProcessOutputFormat OutputFormat { get; private set; }

    /// <summary>
    /// Sets the specified <see cref="IOutputWriter{TOutput}"/> as the main output writer for the process.
    /// There is currently no way to overwrite main input reader, process will use STD IN (Console.In).
    /// </summary>
    /// <param name="mainOut">The main output writer for the process.</param>
    public static void SetIO(IOutputWriter<object> mainOut)
    {
        Console.SetIn(_defaultConsoleInput);
        Console.SetOut(_defaultConsoleOutput);

        Output = new ExternalProcessOutputHtmlWriter(async str => await mainOut.WriteAsync(str));
    }

    /// <summary>
    /// Uses STD IO (default Console.In/Out) as the main IO channel for the process.
    /// </summary>
    public static void UseStandardIO(ExternalProcessOutputFormat format, bool useConsoleColors = true)
    {
        OutputFormat = format;

        if (format == ExternalProcessOutputFormat.HTML)
        {
            Console.SetIn(new ActionTextReader(() =>
            {
                RawConsoleWriteLine("[INPUT_REQUEST]");
                return _defaultConsoleInput.ReadLine();
            }));
            Console.SetOut(new ActionTextWriter((value, appendNewLine) => ResultWrite(value, new DumpOptions(AppendNewLineToAllTextOutput: appendNewLine))));
            Output = new ExternalProcessOutputHtmlWriter(async str => await _defaultConsoleOutput.WriteLineAsync(str));
        }
        else
        {
            Console.SetIn(_defaultConsoleInput);
            Console.SetOut(_defaultConsoleOutput);

            Output = format == ExternalProcessOutputFormat.Console
                ? new ExternalProcessOutputConsoleWriter(useConsoleColors)
                : new ExternalProcessOutputTextWriter(useConsoleColors, async str => await _defaultConsoleOutput.WriteLineAsync(str));
        }
    }

    public static void RawConsoleWriteLine(string text)
    {
        _defaultConsoleOutput.WriteLine(text);
    }

    public static void ResultWrite(object? o, DumpOptions? options = null)
    {
        _ = Output.WriteResultAsync(o, options ?? DumpOptions.Default);
    }

    public static void SqlWrite(object? o, DumpOptions? options = null)
    {
        _ = Output.WriteSqlAsync(o, options);
    }
}
