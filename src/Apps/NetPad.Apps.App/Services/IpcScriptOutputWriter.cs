using System;
using System.Threading.Tasks;
using NetPad.Common;
using NetPad.Events;
using NetPad.Html;
using NetPad.IO;
using NetPad.UiInterop;

namespace NetPad.Services;

public class IpcScriptResultOutputWriter : IpcScriptOutputWriter
{
    public IpcScriptResultOutputWriter(Guid scriptId, IIpcService ipcService) : base(scriptId, ipcService)
    {
    }

    protected override async Task IpcSendAsync(ScriptOutput msg)
    {
        await _ipcService.SendAsync(new ScriptOutputEmittedEvent(_scriptId, JsonSerializer.Serialize(msg)));
    }
}

public class IpcScriptSqlOutputWriter : IpcScriptOutputWriter
{
    public IpcScriptSqlOutputWriter(Guid scriptId, IIpcService ipcService) : base(scriptId, ipcService)
    {
    }

    protected override async Task IpcSendAsync(ScriptOutput msg)
    {
        await _ipcService.SendAsync(new ScriptSqlOutputEmittedEvent(_scriptId, JsonSerializer.Serialize(msg)));
    }
}

public abstract class IpcScriptOutputWriter : IOutputWriter<ScriptOutput>
{
    protected readonly Guid _scriptId;
    protected readonly IIpcService _ipcService;

    protected IpcScriptOutputWriter(Guid scriptId, IIpcService ipcService)
    {
        _scriptId = scriptId;
        _ipcService = ipcService;
    }

    protected abstract Task IpcSendAsync(ScriptOutput msg);

    public async Task WriteAsync(ScriptOutput? output, string? title = null)
    {
        if (output is HtmlScriptOutput htmlScriptOutput)
        {
            await IpcSendAsync(htmlScriptOutput);
        }
        else if (output is ErrorScriptOutput errorScriptOutput)
        {
            await IpcSendAsync(new HtmlScriptOutput(errorScriptOutput.Order, HtmlSerializer.Serialize(errorScriptOutput.Body, title, true)));
        }
        else if (output is RawScriptOutput rawScriptOutput)
        {
            await IpcSendAsync(new HtmlScriptOutput(rawScriptOutput.Order, HtmlSerializer.Serialize(rawScriptOutput.Body, title)));
        }
        else
        {
            await IpcSendAsync(new HtmlScriptOutput(HtmlSerializer.Serialize(output, title)));
        }
    }
}
