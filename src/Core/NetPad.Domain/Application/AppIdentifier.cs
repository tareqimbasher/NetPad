using System;

namespace NetPad.Application;

public class AppIdentifier
{
    public AppIdentifier()
    {
        Name = "NetPad";
        Version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version ?? new();
    }

    public string Name { get; }
    public Version Version { get; }
}
