using System.Linq;
using System.Text.Json;
using ElectronNET.API;
using NetPad.Common;
using NetPad.Utils;

namespace NetPad.BackgroundServices
{
    public abstract class BackgroundService :  Microsoft.Extensions.Hosting.BackgroundService
    {
        protected BrowserWindow? BrowserWindow => ElectronUtil.MainWindow;

        protected string? Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, JsonSerialization.DefaultOptions);
        }
    }
}
