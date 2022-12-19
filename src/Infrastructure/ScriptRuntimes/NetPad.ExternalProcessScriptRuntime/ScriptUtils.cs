using NetPad.IO;
using O2Html;

public static class ScriptUtils
{
    private static IScriptOutputAdapter<object, object>? _output;

    internal static IScriptOutputAdapter<object, object> Output
    {
        get
        {
            _output ??= new ScriptOutputAdapter<object, object>(
                new ExternalProcessOutputWriter(ExternalProcessOutputChannel.Results),
                new ExternalProcessOutputWriter(ExternalProcessOutputChannel.Sql)
            );

            return _output;
        }
    }

    public static void SetIO(IScriptOutputAdapter<object, object>? output)
    {
        _output = output;
    }

    public static void ResultWrite(object? o = null, string? title = null)
    {
        Output.ResultsChannel.WriteAsync(o, title);
    }

    public static void SqlWrite(object? o = null, string? title = null)
    {
        Output.SqlChannel?.WriteAsync(o, title);
    }
}

public static class Extensions
{
    /// <summary>
    /// Dumps this object to the results view.
    /// </summary>
    /// <param name="o">The object being dumped.</param>
    /// <param name="title">An optional title for the result.</param>
    /// <returns>The object being dumped.</returns>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("o")]
    public static T? Dump<T>(this T? o, string? title = null)
    {
        ScriptUtils.ResultWrite(o, title);
        return o;
    }
}

public class ExternalProcessOutputWriter : IOutputWriter<object>
{
    private readonly ExternalProcessOutputChannel _channel;

    public ExternalProcessOutputWriter(ExternalProcessOutputChannel channel)
    {
        _channel = channel;
    }

    public System.Threading.Tasks.Task WriteAsync(object? output, string? title = null)
    {
        var html = Utils.ToHtml(output, title);

        var processOutput = new ExternalProcessOutput<HtmlScriptOutput>(_channel, new HtmlScriptOutput(html));
        var serializedOutput = NetPad.Common.JsonSerializer.Serialize(processOutput);

        Console.WriteLine(serializedOutput);

        return System.Threading.Tasks.Task.CompletedTask;
    }
}

public enum ExternalProcessOutputChannel
{
    Unknown = 0,
    Results = 1,
    Sql = 2
}

public record ExternalProcessOutput<TOutput>(ExternalProcessOutputChannel Channel, TOutput? Output);

internal static class Utils
{
    private static readonly HtmlSerializerSettings _htmlSerializerSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.IgnoreAndSerializeCyclicReference
    };

    public static string ToHtml(object? output, string? title = null)
    {
        var group = new O2Html.Dom.Element("div").WithAddClass("group");

        if (title != null)
        {
            group.WithAddClass("titled")
                .AddAndGetElement("h6")
                .WithAddClass("title")
                .AddText(title);
        }

        O2Html.Dom.Element element;

        try
        {
            element = HtmlConvert.Serialize(output, _htmlSerializerSettings);
        }
        catch (Exception ex)
        {
            element = HtmlConvert.Serialize(ex, _htmlSerializerSettings);
        }

        if (element.Children.All(c => c.Type == O2Html.Dom.NodeType.Text))
            group.WithAddClass("text");

        group.AddChild(element);

        return group.ToHtml();
    }
}
