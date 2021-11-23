using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ElectronNET.API;
using NetPad.Common;
using NetPad.Events;
using NetPad.Runtimes;
using NetPad.Scripts;

namespace NetPad.BackgroundServices
{
    public class IpcScriptOutputWriter : IScriptRuntimeOutputWriter
    {
        public ScriptEnvironment Environment { get; }

        public IpcScriptOutputWriter(ScriptEnvironment environment)
        {
            Environment = environment;
        }

        public Task WriteAsync(object? output)
        {
            var data = new
            {
                ScriptId = Environment.Script.Id,
                Output = output?.ToString()
            };

            Electron.IpcMain.Send(Electron.WindowManager.BrowserWindows.First(),
                nameof(ScriptOutputEmitted),
                JsonSerializer.Serialize(new ScriptOutputEmitted(Environment.Script.Id, output?.ToString()), options: JsonSerializerConfig.DefaultJsonSerializerOptions));

            return Task.CompletedTask;
        }
    }
}
