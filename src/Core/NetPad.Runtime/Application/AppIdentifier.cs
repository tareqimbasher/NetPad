using System.Reflection;
using NJsonSchema;
using NJsonSchema.Annotations;

namespace NetPad.Application;

/// <summary>
/// Provides a centralized identifier and version information for the application.
/// </summary>
/// <remarks>
/// Exposes the human‑readable application name, a GUID‑based application identifier,
/// and resolves the product version at runtime by reading the assembly’s
/// <see cref="AssemblyInformationalVersionAttribute"/> (falling back to the assembly
/// version if none is specified).
/// </remarks>
public class AppIdentifier
{
    /// <summary>
    /// The human‑readable name of the application.
    /// </summary>
    public const string AppName = "NetPad";

    /// <summary>
    /// The unique application identifier (GUID).
    /// </summary>
    public const string AppId = "NETPAD_8C94D5EA-9510-4493-AA43-CADE372ED853";

    /// <summary>
    /// The product version string resolved at runtime.
    /// </summary>
    // ReSharper disable once InconsistentNaming
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
