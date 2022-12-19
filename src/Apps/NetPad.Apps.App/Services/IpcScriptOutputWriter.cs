using System;
using System.Linq;
using System.Threading.Tasks;
using NetPad.Events;
using NetPad.IO;
using NetPad.UiInterop;
using O2Html;
using O2Html.Dom;

namespace NetPad.Services;

public class IpcScriptResultOutputWriter : IpcScriptOutputWriter
{
    public IpcScriptResultOutputWriter(Guid scriptId, IIpcService ipcService) : base(scriptId, ipcService)
    {
    }

    protected override async Task IpcSendAsync(string? msg)
    {
        await _ipcService.SendAsync(new ScriptOutputEmittedEvent(_scriptId, msg));
    }
}

public class IpcScriptSqlOutputWriter : IpcScriptOutputWriter
{
    public IpcScriptSqlOutputWriter(Guid scriptId, IIpcService ipcService) : base(scriptId, ipcService)
    {
    }

    protected override async Task IpcSendAsync(string? msg)
    {
        await _ipcService.SendAsync(new ScriptSqlOutputEmittedEvent(_scriptId, msg));
    }
}

public abstract class IpcScriptOutputWriter : IOutputWriter<ScriptOutput>
{
    protected readonly Guid _scriptId;
    protected readonly IIpcService _ipcService;

    private static readonly HtmlSerializerSettings _htmlSerializerSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.IgnoreAndSerializeCyclicReference,
        DoNotSerializeNonRootEmptyCollections = true
    };

    protected IpcScriptOutputWriter(Guid scriptId, IIpcService ipcService)
    {
        _scriptId = scriptId;
        _ipcService = ipcService;
    }

    protected abstract Task IpcSendAsync(string? msg);

    public async Task WriteAsync(ScriptOutput? output, string? title = null)
    {
        if (output is HtmlScriptOutput htmlScriptOutput)
        {
            await IpcSendAsync(htmlScriptOutput.Body);
        }
        else if (output is RawScriptOutput rawScriptOutput)
        {
            await IpcSendAsync(ToHtml(rawScriptOutput.Body, title));
        }
        else
        {
            await IpcSendAsync(ToHtml(output, title));
        }
    }

    private static string ToHtml(object? output, string? title = null)
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

        return group.ToHtml();
    }
}
