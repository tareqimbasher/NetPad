using System;

namespace NetPad.Application;

public class AppIdentifier
{
    public const string AppName = "NetPad";

    public AppIdentifier()
    {
        Version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version ?? new();
    }

    public string Name => AppName;
    public Version Version { get; }
}
