using System.Threading.Tasks;
using NetPad.Events;
using NetPad.IO;
using NetPad.Scripts;
using NetPad.UiInterop;

namespace NetPad.Services;

public class IpcScriptSqlOutputWriter : IOutputWriter
{
    private readonly ScriptEnvironment _environment;
    private readonly IIpcService _ipcService;

    public IpcScriptSqlOutputWriter(ScriptEnvironment environment, IIpcService ipcService)
    {
        _environment = environment;
        _ipcService = ipcService;
    }

    public async Task WriteAsync(object? output, string? title = null)
    {
        await _ipcService.SendAsync(new ScriptSqlOutputEmittedEvent(_environment.Script.Id, output?.ToString()));
    }
}
