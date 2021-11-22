using System.Linq;
using System.Text.Json;
using ElectronNET.API;
using NetPad.Common;

namespace NetPad.BackgroundServices
{
    public abstract class BackgroundService :  Microsoft.Extensions.Hosting.BackgroundService
    {
        protected BrowserWindow BrowserWindow => Electron.WindowManager.BrowserWindows.First();

        protected string? Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, JsonSerializerConfig.DefaultJsonSerializerOptions);
        }
    }
}
