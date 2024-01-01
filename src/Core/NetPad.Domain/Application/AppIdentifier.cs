using System;
using System.Linq;
using System.Reflection;

namespace NetPad.Application;

public class AppIdentifier
{
    public const string AppName = "NetPad";
    public static readonly Version VERSION;
    public static readonly string PRODUCT_VERSION;

    static AppIdentifier()
    {
        VERSION = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version();

        var infoVersion = (AssemblyInformationalVersionAttribute?)Assembly.GetEntryAssembly()!
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
            .FirstOrDefault();

        PRODUCT_VERSION = infoVersion?.InformationalVersion ?? VERSION.ToString();
    }

    public string Name => AppName;
    public Version Version => VERSION;
    public string ProductVersion => PRODUCT_VERSION;
}
