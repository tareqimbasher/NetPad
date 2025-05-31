using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using NetPad.Presentation;
using NetPad.Presentation.Html;
using O2Html.Dom;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InvokeAsExtensionMethod

namespace NetPad.ExecutionModel.ClientServer.ScriptServices;

/// <summary>
/// Helpers for dumping data, caching, environment access, and more.
/// </summary>
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
    public static UserScript Script { get; internal set; } = null!;

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
    public static MemCache Cache { get; } = new();

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
    /// <param name="language">
    /// See <see href="https://github.com/highlightjs/highlight.js/blob/main/SUPPORTED_LANGUAGES.md">Highlight.js - SUPPORTED_LANGUAGES.md</see>.
    /// </param>
    /// <returns>The input code.</returns>
    public static string DumpCode(string code, string language = "auto")
    {
        Dump(code, code: language);
        return code;
    }

    /// <summary>
    /// Serializes an object to an HTML string.
    /// </summary>
    public static string ToHtmlString<T>(T value, bool indented = false)
    {
        return indented
            ? HtmlPresenter.SerializeToElement(value).ToHtml(O2Html.Formatting.Indented)
            : HtmlPresenter.Serialize(value);
    }

    /// <summary>
    /// Serializes an object to an HTML <see cref="Element"/>.
    /// </summary>
    public static Element ToHtmlElement<T>(T value)
    {
        return HtmlPresenter.SerializeToElement(value);
    }

    /// <summary>
    /// Dumps raw HTML to the results console. Example:
    /// <code>
    /// Util.RawHtml(XElement.Parse("<h1>Heading 1</h1>"));
    /// </code>
    /// </summary>
    public static XElement RawHtml(XElement xElement)
    {
        Dump(TextNode.RawText(xElement.ToString()));
        return xElement;
    }

    /// <summary>
    /// Dumps raw HTML to the results console. Example:
    /// <code>
    /// Util.RawHtml("<h1>Heading 1</h1>");
    /// </code>
    /// </summary>
    public static string RawHtml(string html)
    {
        Dump(TextNode.RawText(html));
        return html;
    }

    /// <summary>
    /// Dumps an object, or value, to the results console.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the object being dumped. Can be a reference or value type.
    /// </typeparam>
    /// <param name="o">The object to dump.</param>
    /// <param name="title">
    /// Optional. A heading displayed above the dumped output to help distinguish multiple dumps.
    /// For example, <c>Dump(person, "Current User")</c> renders a “Current User” heading.
    /// </param>
    /// <param name="css">
    /// Optional. One or more CSS class names to apply to the output container for styling the rendered dump.
    /// You can use standard Bootstrap v5 class names (e.g., <c>"text-success"</c>, <c>"w-25"</c>), or specify custom classes
    /// that you've defined under Settings &gt; Styles.
    /// For example: <c>Dump(obj, css: "card text-bg-warning w-25")</c>
    /// </param>
    /// <param name="code">
    /// Optional. If you’re dumping a code snippet, specify its language (e.g. <c>"csharp"</c>, <c>"json"</c>, <c>"xml"</c>, etc.).
    /// The output will be syntax-highlighted using <see href="https://github.com/highlightjs/highlight.js/blob/main/SUPPORTED_LANGUAGES.md">Highlight.js</see>.
    /// </param>
    /// <param name="clear">
    /// Optional. If provided, the dump will automatically be removed from the console after the given time in milliseconds.
    /// For example, <c>clear: 5000</c> makes it disappear after 5 seconds.
    /// </param>
    /// <returns>
    /// Returns the same object instance (<paramref name="o"/>), allowing you to write:
    /// <code>
    /// var result = GetItems()
    ///     .Where(i => i.IsValid)
    ///     .Dump("Filtered Items")
    ///     .Select(i => i.Value);
    /// </code>
    /// </returns>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("o")]
    public static T? Dump<T>(T? o, string? title = null, string? css = null, string? code = null, int? clear = null)
    {
        return DumpExtension.Dump(o, title, css, code, clear);
    }
    /// <summary>
    /// Dumps an object, or value, to the results console, awaiting the call first.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the object being dumped. Can be a reference or value type.
    /// </typeparam>
    /// <param name="o">The object to dump.</param>
    /// <param name="title">
    /// Optional. A heading displayed above the dumped output to help distinguish multiple dumps.
    /// For example, <c>Dump(person, "Current User")</c> renders a “Current User” heading.
    /// </param>
    /// <param name="css">
    /// Optional. One or more CSS class names to apply to the output container for styling the rendered dump.
    /// You can use standard Bootstrap v5 class names (e.g., <c>"text-success"</c>, <c>"w-25"</c>), or specify custom classes
    /// that you've defined under Settings &gt; Styles.
    /// For example: <c>Dump(obj, css: "card text-bg-warning w-25")</c>
    /// </param>
    /// <param name="code">
    /// Optional. If you’re dumping a code snippet, specify its language (e.g. <c>"csharp"</c>, <c>"json"</c>, <c>"xml"</c>, etc.).
    /// The output will be syntax-highlighted using <see href="https://github.com/highlightjs/highlight.js/blob/main/SUPPORTED_LANGUAGES.md">Highlight.js</see>.
    /// </param>
    /// <param name="clear">
    /// Optional. If provided, the dump will automatically be removed from the console after the given time in milliseconds.
    /// For example, <c>clear: 5000</c> makes it disappear after 5 seconds.
    /// </param>
    /// <returns>
    /// Returns the same object instance (<paramref name="o"/>), allowing you to write:
    /// <code>
    /// var result = await GetItemsAsync()
    ///     .Dump("Filtered Items")
    /// </code>
    /// </returns>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("o")]
    public static async Task<T?> Dump<T>(
        Task<T?> o,
        string? title = null,
        string? css = null,
        string? code = null,
        int? clear = null)
    {
        var result = await o.ConfigureAwait(false);
        return DumpExtension.Dump(result, title, css, code, clear);
    }

    /// <summary>
    /// Dumps an object to the results console.
    /// </summary>
    /// <param name="o">The object to dump.</param>
    /// <param name="options">Dump options.</param>
    /// <returns>The same object being dumped.</returns>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("o")]
    public static T? Dump<T>(T? o, DumpOptions options)
    {
        return DumpExtension.Dump(o, options);
    }

    /// <summary>
    /// Dumps an object to the results console.
    /// </summary>
    /// <param name="o">The object to dump.</param>
    /// <param name="options">Dump options.</param>
    /// <returns>The same object being dumped.</returns>
    public static async Task<T?> Dump<T>(Task<T?> o, DumpOptions options)
    {
        var result = await o.ConfigureAwait(false);
        return DumpExtension.Dump(result, options);
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
    public static ReadOnlySpan<T> Dump<T>(
        ReadOnlySpan<T> span,
        string? title = null,
        string? css = null,
        int? clear = null)
    {
        return DumpExtension.Dump(span, title, css, clear);
    }
}

/// <summary>
/// Information about the current script-host environment.
/// </summary>
public class HostEnvironment(int parentPid)
{
    /// <summary>
    /// The UTC date and time the script-host process started.
    /// </summary>
    public DateTime HostStarted { get; } = DateTime.UtcNow;

    /// <summary>
    /// The process ID (PID) of the script-host process.
    /// </summary>
    public int ProcessPid => Environment.ProcessId;

    /// <summary>
    /// The process ID (PID) of the parent process that started the script-host process.
    /// </summary>
    public int ParentPid => parentPid;

    /// <summary>
    /// The .NET runtime version the script-host process is running on.
    /// </summary>
    public Version DotNetRuntimeVersion => Environment.Version;

    /// <summary>
    /// Gets the name of the .NET installation on which the script-host process is running.
    /// </summary>
    public string FrameworkDescription => RuntimeInformation.FrameworkDescription;

    /// <summary>
    /// Gets the current platform identifier and version number.
    /// </summary>
    public OperatingSystem OSVersion => Environment.OSVersion;

    /// <summary>
    /// Gets the platform on which an app is running.
    /// </summary>
    public string RuntimeIdentifier => RuntimeInformation.RuntimeIdentifier;

    /// <summary>
    /// Gets a string that describes the operating system on which the app is running.
    /// </summary>
    public string OSDescription => RuntimeInformation.OSDescription;

    /// <summary>
    /// Gets the process architecture of the currently running app.
    /// </summary>
    public Architecture ProcessArchitecture => RuntimeInformation.ProcessArchitecture;

    /// <summary>
    /// Gets the platform architecture on which the current app is running.
    /// </summary>
    public Architecture OSArchitecture => RuntimeInformation.OSArchitecture;
}
