using System;
using System.Linq;
using System.Threading.Tasks;
using NetPad.Events;
using NetPad.IO;
using NetPad.Scripts;
using NetPad.UiInterop;
using O2Html;
using O2Html.Dom;

namespace NetPad.Services;

public class IpcScriptOutputWriter : IOutputWriter
{
    private readonly IIpcService _ipcService;
    private static readonly HtmlSerializerSettings _htmlSerializerSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.IgnoreAndSerializeCyclicReference
    };

    public IpcScriptOutputWriter(ScriptEnvironment environment, IIpcService ipcService)
    {
        Environment = environment;
        _ipcService = ipcService;
    }

    public ScriptEnvironment Environment { get; }

    public async Task WriteAsync(object? output, string? title = null)
    {
        var group = new Element("div").WithAddClass("group");

        if (title != null)
        {
            group.WithAddClass("titled")
                .AddAndGetElement("h6")
                .WithAddClass("title")
                .AddText(title);
        }

        Element element;

        try
        {
            element = HtmlConvert.Serialize(output, _htmlSerializerSettings);
        }
        catch (Exception ex)
        {
            element = HtmlConvert.Serialize(ex, _htmlSerializerSettings);
        }

        if (element.Children.All(c => c.Type == NodeType.Text))
            group.WithAddClass("text");

        group.AddChild(element);

        await _ipcService.SendAsync(new ScriptOutputEmitted(Environment.Script.Id, group.ToHtml()));
    }
}
