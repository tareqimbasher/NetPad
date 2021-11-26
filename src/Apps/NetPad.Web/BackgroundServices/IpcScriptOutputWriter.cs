using System.Threading.Tasks;
using NetPad.Events;
using NetPad.Runtimes;
using NetPad.Scripts;
using NetPad.UiInterop;

namespace NetPad.BackgroundServices
{
    public class IpcScriptOutputWriter : IScriptRuntimeOutputWriter
    {
        private readonly IIpcService _ipcService;
        public ScriptEnvironment Environment { get; }

        public IpcScriptOutputWriter(ScriptEnvironment environment, IIpcService ipcService)
        {
            _ipcService = ipcService;
            Environment = environment;
        }

        public async Task WriteAsync(object? output)
        {
            await _ipcService.SendAsync(new ScriptOutputEmitted(Environment.Script.Id, output?.ToString()));
        }
    }
}
