using System.Text.Json;
using NetPad.Common;

namespace NetPad.BackgroundServices
{
    public abstract class BackgroundService :  Microsoft.Extensions.Hosting.BackgroundService
    {
        protected string? Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, JsonSerialization.DefaultOptions);
        }
    }
}
