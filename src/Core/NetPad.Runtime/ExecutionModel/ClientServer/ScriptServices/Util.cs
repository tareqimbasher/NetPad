using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using NetPad.Presentation;
using NetPad.Presentation.Html;
using O2Html.Dom;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InvokeAsExtensionMethod

namespace NetPad.ExecutionModel.ClientServer.ScriptServices;

public static class Util
{
    /// <summary>
    /// <para>
    /// If true a new script-host process will be started for each script run.
    /// </para>
    /// <para>
    /// If this is false (default) a script-host process is started for a particular script the first time it runs and then
    /// remains "alive" and reused for consecutive runs (with some exceptions like when the .NET version is changed on
    /// the script). This allows caching and sharing data between script runs as well as lazy-loading grid data.
    /// When this is true, the script-host process will remain "alive" until the next time the script runs, at which
    /// time, the old script-process is terminated and a new process is started.
    /// </para>
    /// <para>
    /// Note: setting this to true means that data will not persist across script runs, for example the <see cref="Cache"/>
    /// property will no longer retain data from previous script runs.
    /// </para>
    /// </summary>
    public static bool RestartHostOnEveryRun { get; set; }

    /// <summary>
    /// The current script.
    /// </summary>
    public static UserScript Script { get; set; } = null!;

    /// <summary>
    /// Information about the current script-host environment.
    /// </summary>
    public static HostEnvironment Environment { get; set; } = null!;

    /// <summary>
    /// This stopwatch is started when user code starts executing.
    /// </summary>
    public static Stopwatch Stopwatch { get; } = new();

    /// <summary>
    /// A memory-cache that persists between script runs.
    /// </summary>
    public static MemCache Cache => new();

    /// <summary>
    /// Terminates script-host process and current script execution.
    /// </summary>
    public static void Terminate() => System.Environment.Exit(0);

    /// <summary>
    /// Opens a URL with the default application.
    /// </summary>
    public static void OpenUrl(string url) => ProcessUtil.OpenWithDefaultApp(url);

    /// <summary>
    /// Opens a directory with the default application.
    /// </summary>
    /// <param name="path">Path to directory.</param>
    /// <exception cref="DirectoryNotFoundException">If directory doesn't exist.</exception>
    public static void OpenDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory {path} not found");
        }

        ProcessUtil.OpenWithDefaultApp(path);
    }

    /// <summary>
    /// Opens a file with the default application.
    /// </summary>
    /// <param name="path">Path to file.</param>
    /// <exception cref="FileNotFoundException">If file doesn't exist.</exception>
    public static void OpenFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File {path} not found");
        }

        ProcessUtil.OpenWithDefaultApp(path);
    }

    /// <summary>
    /// Formats a code string and dumps it the results console.
    /// </summary>
    /// <param name="code">The code to be formatted.</param>
    /// <param name="language">See https://github.com/highlightjs/highlight.js/blob/main/SUPPORTED_LANGUAGES.md
    /// for supported languages.</param>
    /// <returns></returns>
    public static string DumpCode(string code, string language = "auto")
    {
        Dump(code, code: language);
        return code;
    }

    /// <summary>
    /// Serializes an object to a HTML string.
    /// </summary>
    public static string ToHtmlString<T>(T value, bool indented = false)
    {
        return indented
            ? HtmlPresenter.SerializeToElement(value).ToHtml(O2Html.Formatting.Indented)
            : HtmlPresenter.Serialize(value);
    }

    /// <summary>
    /// Serializes an object to a HTML element.
    /// </summary>
    public static Element ToHtmlElement<T>(T value)
    {
        return HtmlPresenter.SerializeToElement(value);
    }

    /// <summary>
    /// Returns a <see cref="TextNode"/> from the given <see cref="XElement"/>.
    /// </summary>
    public static TextNode RawHtml(XElement xElement)
    {
        return TextNode.RawText(xElement.ToString());
    }

    /// <summary>
    /// Returns a <see cref="TextNode"/> from the given HTML string.
    /// </summary>
    public static TextNode RawHtml(string html)
    {
        return TextNode.RawText(html);
    }

    /// <summary>
    /// Dumps an object to the results console.
    /// </summary>
    /// <param name="o">The object to dump.</param>
    /// <param name="title">If specified, will add a title heading to the result.</param>
    /// <param name="css">If specified, will add the specified CSS classes to the result.</param>
    /// <param name="code">If specified, assumes the dump target is a code string of this language and will
    /// render with syntax highlighting. See https://github.com/highlightjs/highlight.js/blob/main/SUPPORTED_LANGUAGES.md for supported languages.</param>
    /// <param name="clear">If specified, will remove the result after specified milliseconds.</param>
    /// <returns>The same object being dumped.</returns>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("o")]
    public static T? Dump<T>(T? o, string? title = null, string? css = null, string? code = null, int? clear = null)
    {
        return DumpExtension.Dump(o, title, css, code, clear);
    }

    /// <summary>
    /// Dumps an <see cref="Span{T}"/> to the results view.
    /// </summary>
    /// <param name="span">The <see cref="Span{T}"/> to dump.</param>
    /// <param name="title">An optional title for the result.</param>
    /// <param name="css">If specified, will be added as CSS classes to the result.</param>
    /// <param name="clear">If specified, will remove the result after specified milliseconds.</param>
    /// <returns>The <see cref="Span{T}"/> being dumped.</returns>
    public static Span<T> Dump<T>(Span<T> span, string? title = null, string? css = null, int? clear = null)
    {
        return DumpExtension.Dump(span, title, css, clear);
    }

    /// <summary>
    /// Dumps an <see cref="ReadOnlySpan{T}"/> to the results view.
    /// </summary>
    /// <param name="span">The <see cref="ReadOnlySpan{T}"/> to dump.</param>
    /// <param name="title">An optional title for the result.</param>
    /// <param name="css">If specified, will be added as CSS classes to the result.</param>
    /// <param name="clear">If specified, will remove the result after specified milliseconds.</param>
    /// <returns>The <see cref="ReadOnlySpan{T}"/> being dumped.</returns>
    public static ReadOnlySpan<T> Dump<T>(ReadOnlySpan<T> span, string? title = null, string? css = null,
        int? clear = null)
    {
        return DumpExtension.Dump(span, title, css, clear);
    }
}

/// <summary>
/// Information about the current script-host environment
/// </summary>
public class HostEnvironment(int parentPid)
{
    /// <summary>
    /// The date and time the host process started.
    /// </summary>
    public DateTime HostStarted = DateTime.Now;

    /// <summary>
    /// The process ID (PID) of the host process.
    /// </summary>
    public int ProcessPid => Environment.ProcessId;

    /// <summary>
    /// The process ID (PID) of the parent process that started the current host process.
    /// </summary>
    public int ParentPid => parentPid;

    /// <summary>
    /// The .NET runtime version the host is running on.
    /// </summary>
    public Version DotNetRuntimeVersion => Environment.Version;

    /// <summary>
    /// Gets the name of the .NET installation on which the host is running.
    /// </summary>
    public string FrameworkDescription => RuntimeInformation.FrameworkDescription;

    public OperatingSystem OSVersion => Environment.OSVersion;
    public string RuntimeIdentifier => RuntimeInformation.RuntimeIdentifier;
    public string OSDescription => RuntimeInformation.OSDescription;
    public Architecture ProcessArchitecture => RuntimeInformation.ProcessArchitecture;
    public Architecture OSArchitecture => RuntimeInformation.OSArchitecture;
}
