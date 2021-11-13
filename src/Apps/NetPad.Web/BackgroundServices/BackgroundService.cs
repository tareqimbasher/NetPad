using System.Linq;
using System.Text.Json;
using ElectronNET.API;

namespace NetPad.BackgroundServices
{
    public abstract class BackgroundService :  Microsoft.Extensions.Hosting.BackgroundService
    {
        protected readonly JsonSerializerOptions _serializationOptions;

        public BackgroundService()
        {
            _serializationOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        protected BrowserWindow BrowserWindow => Electron.WindowManager.BrowserWindows.First();

        protected string? Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, _serializationOptions);
        }
    }
}
