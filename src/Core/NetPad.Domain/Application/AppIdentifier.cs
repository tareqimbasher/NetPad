using System;
using System.Linq;
using System.Reflection;

namespace NetPad.Application;

public class AppIdentifier
{
    public const string AppName = "NetPad";

    public AppIdentifier()
    {
        Version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version();

        var infoVersion = (AssemblyInformationalVersionAttribute?)Assembly.GetEntryAssembly()!
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
            .FirstOrDefault();

        ProductVersion = infoVersion?.InformationalVersion ?? Version.ToString();
    }

    public string Name => AppName;
    public Version Version { get; }
    public string ProductVersion { get; set; }
}
