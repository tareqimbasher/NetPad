using System.Reflection;
using NJsonSchema;
using NJsonSchema.Annotations;

namespace NetPad.Application;

public class AppIdentifier
{
    public const string AppName = "NetPad";
    public const string AppId = "NETPAD_8C94D5EA-9510-4493-AA43-CADE372ED853";
    public static readonly string PRODUCT_VERSION;

    private static readonly Version _version = Assembly.GetEntryAssembly()?.GetName().Version
                                               ?? throw new Exception(
                                                   $"Entry assembly has no version: {Assembly.GetEntryAssembly()?.GetName()}");

    static AppIdentifier()
    {
        var infoVersion = (AssemblyInformationalVersionAttribute?)Assembly.GetEntryAssembly()!
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
            .FirstOrDefault();

        PRODUCT_VERSION = infoVersion?.InformationalVersion ?? _version.ToString();
    }

    public string Name => AppName;
    [JsonSchema(JsonObjectType.String)] public Version Version => _version;
    public string ProductVersion => PRODUCT_VERSION;
}
