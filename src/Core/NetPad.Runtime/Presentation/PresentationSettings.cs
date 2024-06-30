using System.IO;
using System.Reflection;
using System.Text.Json;

namespace NetPad.Presentation;

public static class PresentationSettings
{
    public const int MaxDepth = 64;
    public const int MaxCollectionLength = 1000;

    public static (uint? maxDepth, uint? maxCollectionSerializeLength) GetConfigFileValues()
    {
        var scriptConfigFilePath  = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "scriptconfig.json"
        );

        if (!File.Exists(scriptConfigFilePath)) return (null, null);

        uint? maxDepth = null;
        uint? maxCollectionSerializeLength = null;

        try
        {
            var json = JsonDocument.Parse(File.ReadAllText(scriptConfigFilePath));

            if (!json.RootElement.TryGetProperty("output", out var outputSettings)) return (null, null);

            if (outputSettings.TryGetProperty("maxDepth", out var prop) && prop.TryGetUInt32(out var md))
            {
                maxDepth = md;
            }

            if (outputSettings.TryGetProperty("maxCollectionSerializeLength", out prop) && prop.TryGetUInt32(out md))
            {
                maxCollectionSerializeLength = md;
            }

            return (maxDepth, maxCollectionSerializeLength);
        }
        catch
        {
            return (null, null);
        }
    }
}
