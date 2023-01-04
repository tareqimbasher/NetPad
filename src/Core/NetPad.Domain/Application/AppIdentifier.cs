using System;
using System.Reflection;

namespace NetPad.Application;

public class AppIdentifier
{
    public const string AppName = "NetPad";

    public AppIdentifier()
    {
        Version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version();
    }

    public string Name => AppName;
    public Version Version { get; }
}
